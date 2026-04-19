using Amazon.Runtime;
using Amazon.S3;
using MediaService.Data;
using MediaService.Infrastructure.Database;
using MediaService.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace MediaService.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddValidation();

        services.Configure<PostgresConfig>(configuration.GetSection(PostgresConfig.SectionName));
        services.Configure<S3Config>(configuration.GetSection(S3Config.SectionName));

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

            options.UseNpgsql(connectionStringBuilder.ConnectionString);
        });

        services.AddSingleton<IAmazonS3>(serviceProvider =>
        {
            var s3Config = serviceProvider
                .GetRequiredService<IOptions<S3Config>>()
                .Value;

            if (string.IsNullOrWhiteSpace(s3Config.Endpoint))
                throw new InvalidOperationException("Missing S3:Endpoint");

            if (string.IsNullOrWhiteSpace(s3Config.Region))
                throw new InvalidOperationException("Missing S3:Region");

            if (string.IsNullOrWhiteSpace(s3Config.AccessKey))
                throw new InvalidOperationException("Missing S3:AccessKey");

            if (string.IsNullOrWhiteSpace(s3Config.SecretKey))
                throw new InvalidOperationException("Missing S3:SecretKey");

            var credentials = new BasicAWSCredentials(
                s3Config.AccessKey,
                s3Config.SecretKey
            );

            var amazonS3Config = new AmazonS3Config
            {
                ServiceURL = s3Config.Endpoint,
                ForcePathStyle = s3Config.ForcePathStyle,
                AuthenticationRegion = s3Config.Region
            };

            return new AmazonS3Client(credentials, amazonS3Config);
        });

        services.AddScoped<MediaService.Application.File.IFileService, MediaService.Application.File.FileService>();
        services.AddScoped<MediaService.Application.Node.NodeService, MediaService.Application.Node.NodeService>();

        return services;
    }
}
