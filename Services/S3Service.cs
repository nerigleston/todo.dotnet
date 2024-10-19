using Amazon.S3.Transfer;
using Amazon.S3.Model;
using Amazon.S3;
using DotNetEnv;

namespace ToDoList.Services
{
    public class S3Service
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;

        public S3Service(IAmazonS3 s3Client, IConfiguration config)
        {
            Env.Load();
            _s3Client = s3Client;
            _bucketName = Environment.GetEnvironmentVariable("AWS_BUCKET_NAME")
                ?? throw new ArgumentNullException("AWS_BUCKET_NAME", "Bucket name is not configured.");
        }

        public async Task<string> UploadFileAsync(IFormFile file, string fileName)
        {
            try
            {
                var transferUtility = new TransferUtility(_s3Client);

                using (var stream = file.OpenReadStream())
                {
                    var request = new TransferUtilityUploadRequest
                    {
                        InputStream = stream,
                        Key = fileName,
                        BucketName = _bucketName,
                        ContentType = file.ContentType
                    };

                    await transferUtility.UploadAsync(request);
                }

                return fileName;
            }
            catch (AmazonS3Exception ex)
            {
                throw new Exception($"Error encountered on server. Message:'{ex.Message}'", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred when uploading the file: {ex.Message}", ex);
            }
        }


        public string GeneratePresignedUrl(string objectKey)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = objectKey,
                Expires = DateTime.UtcNow.AddMinutes(60),
                Verb = HttpVerb.GET
            };

            var url = _s3Client.GetPreSignedURL(request);
            return url;
        }
    }
}
