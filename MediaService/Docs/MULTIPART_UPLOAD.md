# Multipart Upload Complete Flow

## Overview

Multipart upload addresses this scenario:

- Client wants to upload a large file
- API creates local metadata
- API initiates multipart upload on S3
- API generates presigned URLs per part
- Client uploads parts directly to S3
- Client reports ETags
- API completes multipart on S3
- API validates final object
- Database marks file as completed

### Involved Tables

These tables from the schema participate:

- `nodes`
- `files`
- `file_upload_sessions`
- `multipart_uploads`
- `multipart_upload_parts`
- `file_upload_events`

Your schema already separates upload sessions, multipart-specific data, parts, and audit events.

---

## 1. Key Difference vs. Single Upload

**Single Upload:** S3 doesn't create any resource until the client performs `PUT`.

**Multipart Upload:** The API creates a real S3 session:

```
CreateMultipartUpload → S3
```

S3 returns an `uploadId`, stored in:
```
multipart_uploads.storage_upload_id
```

This `uploadId` is critical for:
- `UploadPart` requests
- `CompleteMultipartUpload` requests
- `AbortMultipartUpload` requests

---

## 2. State Machine

Multipart uses an explicit "uploading" state:

```
pending
  ├── uploading
  │     ├── completed ✓
  │     ├── failed
  │     ├── canceled
  │     └── expired
  ├── failed
  ├── canceled
  └── expired
```

### State Meanings

| State | Meaning |
|-------|---------|
| `pending` | Local metadata created, but S3 `uploadId` not yet attached |
| `uploading` | Multipart upload exists on S3, client can upload parts |
| `completed` | S3 assembled final file and DB validated it |
| `failed` | Permanent error |
| `canceled` | Canceled by client/system |
| `expired` | Session expired |

---

## 3. Initialization Endpoint

Recommended endpoint:

```http
POST /files/uploads/multipart
Content-Type: application/json

{
  "parentId": "7f0e8c5d-0f5e-4f3e-9a31-7f2f582c3b25",
  "fileName": "video.mp4",
  "mimeType": "video/mp4",
  "size": 8589934592
}
```

---

## 4. Initial Validations (C#)

Before touching database or S3:

- `fileName` is not empty
- `mimeType` is not empty
- `size > 0`
- `size > single_upload_max_size` (file too large for single upload)
- `size <= max_file_size` (not exceeding system limit)
- `parentId` exists if provided
- `parentId` is an active folder

If the file fits in single upload, don't route through multipart unless explicitly allowed.

---

## 5. Calculating Part Size and Part Count

S3 constraints:
- Maximum 10,000 parts per upload
- Each part: 5 MiB to 5 GiB
- Last part can be less than 5 MiB

**Formula:**
```
part_size = base size per part
parts_count = ceil(file_size / part_size)
```

### Example

```
file_size = 100 MiB
part_size = 16 MiB
parts_count = 7

parts 1-6: 16 MiB each
part 7:    4 MiB
```

### Algorithm (C#)

```csharp
private const long Mib = 1024 * 1024;

private static long DivideRoundUp(long value, long divisor)
{
    return (value + divisor - 1) / divisor;
}

private static long RoundUpToMib(long value)
{
    return DivideRoundUp(value, Mib) * Mib;
}

private static long CalculatePartSize(
    long fileSize,
    long defaultPartSize,
    long minimumPartSize,
    long maximumPartSize,
    int maximumPartsCount)
{
    var requiredPartSize = DivideRoundUp(fileSize, maximumPartsCount);
    var selectedPartSize = Math.Max(defaultPartSize, requiredPartSize);
    selectedPartSize = Math.Max(selectedPartSize, minimumPartSize);
    selectedPartSize = RoundUpToMib(selectedPartSize);

    if (selectedPartSize > maximumPartSize)
    {
        throw new InvalidOperationException("File is too large for multipart upload.");
    }

    return selectedPartSize;
}

private static int CalculatePartsCount(long fileSize, long partSize)
{
    return checked((int)DivideRoundUp(fileSize, partSize));
}
```

---

## 6. Phase 1: Create Local Metadata

C# generates:

```
nodeId
objectKey
expiresAt
partSize
partsCount
```

**Example values:**
```
nodeId = 7e52...
objectKey = files/7e52.../original.mp4
expiresAt = now + 30 minutes
partSize = 16 MiB
partsCount = 512
```

Then call database function:

```sql
create_upload_session(..., upload_mode = 'multipart')
```

Database must atomically insert in **a single transaction**:

### Into `nodes`
- `id` = nodeId
- `parent_id` = parentId
- `name` = fileName
- `type` = 'file'

### Into `files`
- `node_id` = nodeId
- `mime_type` = mimeType
- `size` = size
- `object_key` = objectKey
- `status` = 'pending'

### Into `file_upload_sessions`
- `id` = sessionId
- `node_id` = nodeId
- `upload_mode` = 'multipart'
- `status` = 'pending'
- `expires_at` = expiresAt

### Into `file_upload_events`
- `from_status` = null
- `to_status` = 'pending'
- `reason` = 'Multipart upload session created'

---

## 7. Phase 2: Create Multipart Upload on S3

After creating local metadata, C# calls:

```csharp
var uploadResponse = await _s3Client.InitiateMultipartUploadAsync(
    new InitiateMultipartUploadRequest
    {
        BucketName = _bucketName,
        Key = objectKey,
        ContentType = mimeType
    });

var storageUploadId = uploadResponse.UploadId;
```

S3 responds with `UploadId`, which is stored in:
```
multipart_uploads.storage_upload_id
```

---

## 8. Phase 3: Attach Multipart Upload in Database

C# calls database function:

```sql
attach_multipart_upload(
    sessionId,
    storageUploadId,
    partSize,
    partsCount
)
```

Database must atomically execute in a transaction:

### Insert into `multipart_uploads`
- `session_id` = sessionId
- `storage_upload_id` = storageUploadId
- `part_size` = partSize
- `parts_count` = partsCount

### Update `file_upload_sessions`
- `status` = 'uploading'

### Update `files`
- `status` = 'uploading'

### Insert into `file_upload_events`
- `from_status` = 'pending'
- `to_status` = 'uploading'
- `reason` = 'Multipart upload attached'
- `metadata` = { storageUploadId, partSize, partsCount }

---

## 9. Phase 4: Generate Presigned URLs Per Part

With the `storageUploadId`, C# generates presigned URLs for each `partNumber`.

You can respond with all URLs at once or request them in batches.

### Option A: All URLs at Once

Good for medium-sized files:

```http
POST /files/uploads/multipart

{
  "nodeId": "7e52...",
  "sessionId": "b2f6...",
  "uploadMode": "multipart",
  "partSize": 16777216,
  "partsCount": 5,
  "expiresAt": "2026-05-16T20:00:00Z",
  "parts": [
    {
      "partNumber": 1,
      "uploadUrl": "https://..."
    },
    {
      "partNumber": 2,
      "uploadUrl": "https://..."
    }
  ]
}
```

### Option B: URLs on Demand (Recommended)

Better for large files:

```http
POST /files/uploads/{sessionId}/multipart/parts/urls
Content-Type: application/json

{
  "partNumbers": [1, 2, 3, 4, 5]
}
```

Response:

```json
{
  "parts": [
    {
      "partNumber": 1,
      "uploadUrl": "https://..."
    }
  ]
}
```

**Use Option B** to avoid generating hundreds or thousands of URLs at once.

---

## 10. Phase 5: Client Uploads Parts to S3

The client splits the file by:
- `partSize`
- `partsCount`

For each part:

```csharp
var partStart = (partNumber - 1) * partSize;
var partEnd = Math.Min(partStart + partSize, fileSize);
var partData = await file.ReadAsync(partStart, partEnd - partStart);
```

Then uploads:

```http
PUT {partUploadUrl}
Content-Type: application/octet-stream

[part binary data]
```

S3 responds with an `ETag` header. **The client must preserve this ETag.**

To complete a multipart upload, S3 requires a list of `PartNumber` + `ETag` pairs. S3 concatenates them in ascending order.

---

## 11. Phase 6: Register Uploaded Part

After uploading each part, the client reports:

```http
POST /files/uploads/{sessionId}/multipart/parts
Content-Type: application/json

{
  "partNumber": 1,
  "etag": "\"abc123\"",
  "size": 16777216
}
```

C# calls database:

```sql
register_multipart_upload_part(
    sessionId,
    partNumber,
    etag,
    size
)
```

Database must validate:

- Session exists
- `session.status = 'uploading'`
- `multipart_upload` exists
- `partNumber` is between 1 and `partsCount`
- `etag` is not empty
- `size > 0`

Then insert or update:

```sql
INSERT INTO multipart_upload_parts (session_id, part_number, etag, size)
VALUES (...)
ON CONFLICT (session_id, part_number) DO UPDATE SET etag = ..., size = ...;
```

**Do NOT insert an audit event per part.** That clutters audit logs. Parts themselves are the progress tracking.

---

## 12. Upload Progress

Calculate progress:

```sql
SELECT
    COUNT(*) as uploaded_parts,
    multipart_uploads.parts_count
FROM multipart_uploads
LEFT JOIN multipart_upload_parts
    ON multipart_upload_parts.session_id = multipart_uploads.session_id
WHERE multipart_uploads.session_id = @sessionId
GROUP BY multipart_uploads.parts_count;
```

Or calculate bytes uploaded:

```sql
SELECT COALESCE(SUM(size), 0) as bytes_uploaded
FROM multipart_upload_parts
WHERE session_id = @sessionId;
```

---

## 13. Phase 7: Complete Multipart Upload

Endpoint:

```http
POST /files/uploads/{sessionId}/multipart/complete
```

### Workflow

1. C# queries database to validate parts
2. Database verifies all expected parts exist
3. C# retrieves `partNumber + ETag` ordered by part number
4. C# calls `CompleteMultipartUpload` on S3
5. C# performs `HEAD Object` on the final object key
6. C# validates size and MIME type match
7. C# calls `complete_upload_session()`
8. Database marks session and file as completed

### Database Part Validation

Validate:
- `COUNT(parts) == partsCount`
- Part numbers exist from 1 to `partsCount`
- No missing parts

**Don't just count parts.** Verify the sequence. These parts:
```
1, 2, 4
```
are missing part 3 and would fail.

---

## 14. Sending Completion Request to S3

C# sends to S3 (via AWS SDK):

```csharp
var completeRequest = new CompleteMultipartUploadRequest
{
    BucketName = _bucketName,
    Key = objectKey,
    UploadId = storageUploadId,
    PartETags = parts.Select(p => new PartETag 
    { 
        PartNumber = p.PartNumber, 
        ETag = p.ETag 
    }).ToList()
};

var completeResponse = await _s3Client.CompleteMultipartUploadAsync(completeRequest);
```

Parts **must be ordered by `partNumber`** ascending.

After `CompleteMultipartUpload`, S3 assembles the final object.

---

## 15. Final Validation Against S3

After completing, C# must perform `HEAD Object`:

```csharp
var response = await _s3Client.GetObjectMetadataAsync(
    new GetObjectMetadataRequest
    {
        BucketName = _bucketName,
        Key = objectKey
    });
```

Validate:
- ✅ `ContentLength == files.size`
- ✅ `ContentType` compatible with `files.mime_type`

**If validation fails:**
- Do **not** mark as completed
- Mark as failed or leave in a recoverable state
- **Never mark as successful without this check**

---

## 16. Completing in Database

C# calls:

```sql
complete_upload_session(
    sessionId,
    contentLength,
    contentType,
    etag,
    partsCount,
    metadata)
```

Database must atomically execute:

### Update `file_upload_sessions`
- `status` = 'completed'

### Update `files`
- `status` = 'completed'

### Insert `file_upload_events`
- `from_status` = 'uploading'
- `to_status` = 'completed'
- `reason` = 'Multipart upload completed'
- `metadata` = { contentLength, contentType, etag, partsCount }

### Response

```json
{
  "nodeId": "7e52...",
  "status": "completed"
}
```

---

## 17. Canceling Multipart Upload

Endpoint:

```http
POST /files/uploads/{sessionId}/cancel
```

### Workflow

1. C# queries session for:
   - `objectKey`
   - `storageUploadId`
   - `status`

2. If `status = 'uploading'`:
   - C# calls `AbortMultipartUpload` on S3

3. C# calls database:
   ```sql
   finish_upload_session(
       sessionId,
       'canceled',
       reason,
       metadata)
   ```

4. Database updates:
   - `session.status` = 'canceled'
   - `files.status` = 'canceled'
   - Insert audit event

**Order is critical:**
1. **S3 first** (abort multipart)
2. **Database second** (mark as canceled)

If you mark DB first and S3 abort fails, you leave garbage in S3.

---

## 18. Expiring Multipart Uploads

A worker processes periodically:

```sql
SELECT * FROM file_upload_sessions
WHERE status IN ('pending', 'uploading')
  AND expires_at <= now()
LIMIT 100;
```

### Workflow

1. Worker queries database for expired sessions
2. Database marks:
   - `file_upload_sessions.status` = 'expired'
   - `files.status` = 'expired'
   - Insert audit event
3. Database returns expired sessions with:
   - `sessionId`
   - `uploadMode`
   - `objectKey`
   - `storageUploadId`

4. For each expired multipart session:
   - C# calls `AbortMultipartUpload` on S3

### AWS Lifecycle Rules

Also configure S3 lifecycle rules to auto-abort incomplete multipart uploads:

```json
{
  "Rules": [
    {
      "Id": "AbortIncompleteMultipartUpload",
      "Status": "Enabled",
      "AbortIncompleteMultipartUpload": {
        "DaysAfterInitiation": 7
      }
    }
  ]
}
```

AWS recommends cleaning incomplete uploads to prevent storage waste.

---

## 19. Handling Critical Failures

### Failure Before Creating Metadata

**Nothing to clean up.** No action required.

### Failure After Creating Metadata, Before CreateMultipartUpload

Database has `pending` session.

C# must mark as `failed`.

### Failure After CreateMultipartUpload, Before Attaching to DB (Critical!)

S3 has `uploadId`.
Database doesn't know about it.

**C# must:**
1. Call `AbortMultipartUpload` on S3
2. Mark session `failed` in database

This is one of the most important points in the flow.

### Failure During Part Registration

Do **not** complete multipart.

Client can retry registering the part.

### Failure in CompleteMultipartUpload

**If transient:**
- Keep session as `uploading`
- Allow retry

**If permanent:**
- Call `AbortMultipartUpload` if needed
- Call `finish_upload_session(..., 'failed')`

Don't mark failed for every timeout. It might be recoverable.

---

## 20. Part Retries

One of the main advantages of multipart:

If part 27 fails uploading:
- Do **not** restart entire upload
- Generate new presigned URL for `partNumber = 27`
- Client re-uploads only that part
- Update `multipart_upload_parts` with new ETag

---

## 21. Complete Flow Summary

### INITIALIZE MULTIPART

```
Client → API:
  POST /files/uploads/multipart

C#:
  - Validate request
  - Calculate nodeId
  - Calculate objectKey
  - Calculate expiresAt
  - Calculate partSize and partsCount

Database:
  - Create node/file/session/event in pending

C# → S3:
  - CreateMultipartUpload

C# → Database:
  - attach_multipart_upload
  - session/file transition to uploading

C#:
  - Generate presigned URLs per part

API → Client:
  - sessionId, partSize, partsCount, URLs
```

### UPLOAD PARTS

```
Client → S3:
  - PUT each part

S3 → Client:
  - ETag

Client → API:
  - Report partNumber + ETag + size

API → Database:
  - register_multipart_upload_part
```

### COMPLETE

```
Client → API:
  - POST /files/uploads/{sessionId}/multipart/complete

API → Database:
  - Validate parts exist and complete
  - Get parts ordered by number

API → S3:
  - CompleteMultipartUpload

API → S3:
  - HEAD Object

API → Database:
  - complete_upload_session

API → Client:
  - 200 OK (completed)
```

---

## 22. Anti-Patterns: What NOT to Do

❌ Mark completed just because client sent all ETags
❌ Complete in DB before CompleteMultipartUpload in S3
❌ Generate 10,000 URLs if you can request on-demand
❌ Insert audit event per part (clutters logs)
❌ Leave multipart upload without Abort when canceling
❌ Allow two active sessions for the same `node_id`
❌ Skip S3 HEAD Object validation before marking completed

---

## 23. Design Verdict

Multipart is complex because **two states must coordinate:**
- State in PostgreSQL
- Temporary state in S3

**Proper separation:**

### C# Responsibilities
- Calculate part partitions
- Control S3 operations
- Generate presigned URLs
- Complete/abort multipart operations
- Validate final object against S3

### PostgreSQL Responsibilities
- Create transactional metadata
- Store `uploadId`
- Track uploaded parts
- Validate part completeness
- Change state transitions
- Record audit trail

This division:
✅ Keeps S3 logic out of the database
✅ Keeps fragile multi-step operations in application code
✅ Prevents data consistency issues
✅ Enables proper error recovery

