using System.Text.Json;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using MediaService.Application.Files;
using MediaService.Domain.Files;
using MediaService.Domain.Nodes;
using MediaService.Infrastructure.Database;
using MediaService.Infrastructure.Persistence;
using MediaService.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace MediaService.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddValidation();

        // json policy to camelCase for consistency with JavaScript clients
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });

        services
            .AddOptions<UploadOptions>()
            .Bind(configuration.GetSection(UploadOptions.SectionName))
            .Validate(fileConfig => fileConfig.UploadExpiration > TimeSpan.Zero,
                "Files:UploadExpiration must be greater than zero.")
            .Validate(fileConfig => fileConfig.UploadExpiration <= TimeSpan.FromHours(24),
                "Files:DownloadExpiration must not be greater than 24 hours.")
            .Validate(fileConfig => fileConfig.DownloadExpiration > TimeSpan.Zero,
                "Files:DownloadExpiration must be greater than zero.")
            .Validate(fileConfig => fileConfig.DownloadExpiration <= TimeSpan.FromHours(24),
                "Files:DownloadExpiration must not be greater than 24 hours.")
            .Validate(fileConfig => fileConfig.MaxPartsCount > 0 && fileConfig.MaxPartsCount <= 10_000,
                "Files:MaxPartsCount must be between 1 and 10000.")
            .Validate(fileConfig => fileConfig.SingleUploadMaxSizeMib > 0,
                "Files:SingleUploadMaxSizeMib must be greater than zero.")
            .Validate(fileConfig => fileConfig.DefaultPartSizeMib >= 5 && fileConfig.DefaultPartSizeMib <= 10240,
                "Files:DefaultPartSizeGib must be between 5 MiB and 10240 MiB (10 GiB).")
            .Validate(fileConfig => fileConfig.MinPartSizeMib >= 5 && fileConfig.MinPartSizeMib <= 10240,
                "Files:MinPartSizeMib must be between 5 MiB and 10240 MiB (10 GiB).")
            .Validate(fileConfig => fileConfig.MaxPartSizeMib >= 5 && fileConfig.MaxPartSizeMib <= 10240,
                "Files:MaxPartSizeGib must be between 5 MiB and 10240 MiB (10 GiB).")
            .Validate(fileConfig => fileConfig.MinPartSize <= fileConfig.DefaultPartSize && fileConfig.DefaultPartSize <= fileConfig.MaxPartSize,
                "Files:DefaultPartSizeMiB must be between Files:MinPartSizeMiB and Files:MaxPartSizeMiB.")
            .ValidateOnStart();

        services.Configure<PostgresConfig>(configuration.GetSection(PostgresConfig.SectionName));
        services.Configure<S3Options>(configuration.GetSection(S3Options.SectionName));

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            var postgresConfig = serviceProvider
                .GetRequiredService<IOptions<PostgresConfig>>()
                .Value;

            if (string.IsNullOrWhiteSpace(postgresConfig.Host))
                throw new InvalidOperationException("Missing Postgres:Host");

            if (postgresConfig.Port <= 0)
                throw new InvalidOperationException("Invalid Postgres:Port");

            if (string.IsNullOrWhiteSpace(postgresConfig.Database))
                throw new InvalidOperationException("Missing Postgres:Database");

            if (string.IsNullOrWhiteSpace(postgresConfig.User))
                throw new InvalidOperationException("Missing Postgres:User");

            if (string.IsNullOrWhiteSpace(postgresConfig.Password))
                throw new InvalidOperationException("Missing Postgres:Password");

            var connectionStringBuilder = new NpgsqlConnectionStringBuilder
            {
                Host = postgresConfig.Host,
                Port = postgresConfig.Port,
                Database = postgresConfig.Database,
                Username = postgresConfig.User,
                Password = postgresConfig.Password
            };

            options.UseNpgsql(connectionStringBuilder.ConnectionString,
                npgsqlOptions =>
                {
                    npgsqlOptions
                        .MapEnum<NodeType>("node_type")
                        .MapEnum<UploadStatus>("upload_status");
                });
        });

        services.AddSingleton<IAmazonS3>(serviceProvider =>
        {
            var s3Options = serviceProvider
                .GetRequiredService<IOptions<S3Options>>()
                .Value;

            if (string.IsNullOrWhiteSpace(s3Options.BucketName))
                throw new InvalidOperationException("Missing S3:BucketName");

            if (string.IsNullOrWhiteSpace(s3Options.AccessKey))
                throw new InvalidOperationException("Missing S3:AccessKey");

            if (string.IsNullOrWhiteSpace(s3Options.SecretKey))
                throw new InvalidOperationException("Missing S3:SecretKey");

            var credentials = new BasicAWSCredentials(
                s3Options.AccessKey,
                s3Options.SecretKey
            );

            var s3Config = new AmazonS3Config
            {
                ForcePathStyle = s3Options.ForcePathStyle
            };

            if (!string.IsNullOrWhiteSpace(s3Options.Endpoint))
            {
                s3Config.ServiceURL = s3Options.Endpoint;
                s3Config.AuthenticationRegion = s3Options.Region;
            }
            else
            {
                s3Config.RegionEndpoint = RegionEndpoint.GetBySystemName(s3Options.Region);
            }

            return new AmazonS3Client(credentials, s3Config);
        });

        services.AddSingleton<MediaService.Application.Storage.IStorageService, MediaService.Infrastructure.Storage.StorageService>();

        // repositories
        services.AddScoped<MediaService.Application.Nodes.INodeRepository, MediaService.Infrastructure.Persistence.Repositories.NodeRepository>();
        services.AddScoped<MediaService.Application.Files.IFileRepository, MediaService.Infrastructure.Persistence.Repositories.FileRepository>();

        // services
        services.AddScoped<MediaService.Application.Files.IFileService, MediaService.Application.Files.FileService>();
        services.AddScoped<MediaService.Application.Nodes.INodesService, MediaService.Application.Nodes.NodesService>();

        return services;
    }
}
