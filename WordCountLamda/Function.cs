using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Model;
using Newtonsoft.Json;
using NUnit.Framework;
using NUnit.Framework.Internal.Execution;
using System.Text;
using System.Text.RegularExpressions;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace WordCountLamda;

public class Function
{

    //private readonly IAmazonS3 _s3Client;

    //public Function()
    //{
    //    _s3Client = new AmazonS3Client();
    //}

    //public async Task FunctionHandler(S3Event evnt, ILambdaContext context)
    //{
    //    foreach (var record in evnt.Records)
    //    {
    //        var bucketName = record.S3.Bucket.Name;
    //        var objectKey = record.S3.Object.Key;

    //        try
    //        {
    //            var text = await ReadObjectFromS3Async(bucketName, objectKey);
    //            var wordCount = CountWords(text);
    //            var json = JsonConvert.SerializeObject(wordCount);

    //            var jsonKey = objectKey.Substring(0, objectKey.LastIndexOf('.')) + ".json";

    //            await UploadObjectToS3Async(json, bucketName, jsonKey);
    //        }
    //        catch (Exception ex)
    //        {
    //            context.Logger.LogLine($"Error processing object {objectKey} from bucket {bucketName}. {ex.Message}");
    //        }
    //    }
    //}

    //private async Task<string> ReadObjectFromS3Async(string bucketName, string objectKey)
    //{
    //    var request = new GetObjectRequest
    //    {
    //        BucketName = bucketName,
    //        Key = objectKey
    //    };

    //    using (var response = await _s3Client.GetObjectAsync(request))
    //    using (var responseStream = response.ResponseStream)
    //    using (var reader = new StreamReader(responseStream))
    //    {
    //        return await reader.ReadToEndAsync();
    //    }
    //}

    //private async Task UploadObjectToS3Async(string text, string bucketName, string objectKey)
    //{
    //    var request = new PutObjectRequest
    //    {
    //        BucketName = bucketName,
    //        Key = objectKey,
    //        ContentBody = text
    //    };

    //    await _s3Client.PutObjectAsync(request);
    //}

    //private Dictionary<string, int> CountWords(string text)
    //{
    //    var wordCount = new Dictionary<string, int>();
    //    var words = Regex.Matches(text, @"[\w']+");

    //    foreach (Match word in words)
    //    {
    //        var wordStr = word.Value.ToLower();
    //        if (wordCount.ContainsKey(wordStr))
    //            wordCount[wordStr]++;
    //        else
    //            wordCount[wordStr] = 1;
    //    }

    //    return wordCount;
    //}

    private readonly IAmazonS3 _s3Client;

    public Function()
    {
        _s3Client = new AmazonS3Client();
    }

    public async Task<string> FunctionHandler(string text, ILambdaContext context)
    {
        var bucketName = "my-word-count-bucket"; // Replace with your bucket name
        var objectKey = GenerateObjectKey();
        var sb = new StringBuilder(text);

        try
        {
            await UploadFileToS3Async(text, bucketName, objectKey+".txt");
            var json = GenerateWordCountJson(sb);
            await UploadFileToS3Async(json, bucketName, objectKey+".json");
            return $"Text uploaded successfully to s3://{bucketName}/{objectKey}";
        }
        catch (Exception ex)
        {
            context.Logger.LogLine($"Error uploading text to S3. {ex.Message}");
            throw;
        }
    }


    private async Task UploadFileToS3Async(string text, string bucketName, string objectKey)
    {
        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            ContentBody = text
        };

        await _s3Client.PutObjectAsync(request);
    }
    public string GenerateWordCountJson(StringBuilder text)
    {
        try
        {
            return "";
            // Split the text into words and count their occurrences
            var wordCount = new Dictionary<string, int>();

            // Split the text into words based on whitespace characters
            string[] words = text.ToString().Split(new char[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string word in words)
            {
                // Convert the word to lowercase and remove any leading or trailing whitespace
                string trimmedWord = word.Trim().ToLower();

                if (wordCount.ContainsKey(trimmedWord))
                {
                    wordCount[trimmedWord]++;
                }
                else
                {
                    wordCount[trimmedWord] = 1;
                }
            }

            // Convert the word count dictionary to JSON
            return JsonConvert.SerializeObject(wordCount);
        }
        catch (Exception)
        {

        }

        return "";
    }
    private string GenerateObjectKey()
    {
        // Generate a unique object key based on current timestamp
        return $"text_{DateTime.UtcNow:yyyyMMddHHmmss}";
    }

}