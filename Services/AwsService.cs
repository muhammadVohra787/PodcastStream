using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.S3;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;

namespace Podcast_MVC.Services
{
    public class AwsService
    {
        private static AwsService _instance;
        private static readonly object _lock = new();

        private readonly IAmazonS3 _s3Client;
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly DynamoDBContext _dbContext;

        private readonly IAmazonSimpleSystemsManagement _ssmClient;
        public IAmazonSimpleSystemsManagement SsmClient => _ssmClient;
        public IAmazonS3 S3Client => _s3Client;
        public IAmazonDynamoDB DynamoDbClient => _dynamoDbClient;
        public DynamoDBContext DbContext => _dbContext;

        public string BucketName => "vohra-podcast-files";
        public string TableName => "PodcastEpisodesComments";

        private AwsService(IConfiguration configuration)
        {
            var region = RegionEndpoint.GetBySystemName(configuration["AWS:Region"]);

            AWSCredentials credentials;

            var accessKey = configuration["AWS:AWSAccessKey"];
            var secretKey = configuration["AWS:AWSSecretKey"];

            if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
            {
                // Use explicit keys (for local dev)
                credentials = new BasicAWSCredentials(accessKey, secretKey);
            }
            else
            {
                // Use default credentials chain (for EB / EC2 instance role)
                credentials = FallbackCredentialsFactory.GetCredentials();
            }

            // Initialize AWS clients
            _s3Client = new AmazonS3Client(credentials, region);
            _dynamoDbClient = new AmazonDynamoDBClient(credentials, region);
            _dbContext = new DynamoDBContext(_dynamoDbClient);
            _ssmClient = new AmazonSimpleSystemsManagementClient(credentials, region);
        }

        public static AwsService GetInstance(IConfiguration configuration)
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                        _instance = new AwsService(configuration);
                }
            }
            return _instance;
        }

        public async Task<string> GetParameterAsync(string name, bool decrypt = false)
        {
            var request = new GetParameterRequest
            {
                Name = name,
                WithDecryption = decrypt
            };
            var response = await _ssmClient.GetParameterAsync(request);
            return response.Parameter.Value;
        }
    }
}
