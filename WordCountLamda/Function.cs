using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Model;
using Newtonsoft.Json;
using NUnit.Framework;
using NUnit.Framework.Internal.Execution;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace WordCountLamda;

public class Function
{
    private readonly IAmazonS3 _s3Client;

    public Function()
    {
        _s3Client = new AmazonS3Client();
    }
    /// <summary>
    /// Saves in s3 bucket how many words are mapped in the file of the routing you receive and how many times each word appears
    /// </summary>
    /// <param name="myFile"></param>
    /// <param name="context"></param>
    /// <returns>If the file already exists returns the contents of the saved file</returns>
    //public async Task<string> FunctionCounter(string myFile, ILambdaContext context)
    //{
    //    // - עובד רק לוקאלי מקבלת ניתוב
    //    var bucketName = "my-word-count-bucket";
    //    FileStream fs = new FileStream(myFile,
    //                                   FileMode.Open,
    //                                   FileAccess.Read);

    //    StreamReader reader = new StreamReader(fs);
    //    string fileName = Path.GetFileName(myFile);
    //    string[] parts = fileName.Split('.');
    //    string fileNameWithoutExtension = parts[0];
    //    string str = reader.ReadToEnd();

    //    try
    //    {
    //        var exisitingFile = await DoesObjectExist(bucketName, fileNameWithoutExtension + ".json");
    //        if (exisitingFile != "not exisit")
    //        {
    //            context.Logger.LogLine($"the file exists");
    //            return exisitingFile;
    //        }
    //        var json = GenerateWordCountJson(str);
    //        await UploadFileToS3Async(json, bucketName, fileNameWithoutExtension + ".json");
    //        return $"Text uploaded successfully to s3://{bucketName}/{fileNameWithoutExtension}";
    //    }
    //    catch (Exception ex)
    //    {
    //        context.Logger.LogLine($"Error uploading text to S3. {ex.Message}");
    //        return $"Error uploading text to S3. {ex.Message}";
    //    }

    //    return str;

    //}
    /// <summary>
    ///If myFile exisits in my-word-count-bucket-
    ///this function uploads to my-word-count-bucket a JSON file containing the number of words and how many times each word appears in myFile
    ///otherwise warns of an error
    /// </summary>
    /// <param name="myFile"></param>
    /// <param name="context"></param>
    /// <returns>Returns an appropriate message if it succeeded in loading a cube or if it failed</returns>
    public async Task<string> FunctionCounter(string myFile, ILambdaContext context)
    {

        var bucketName = "my-word-count-bucket";

        try
        {
            var text = await getText(bucketName, myFile, context);
            var json = GenerateWordCountJson(text);
            string[] parts = myFile.Split('.');
            string fileNameWithoutExtension = parts[0];
            await UploadFileToS3Async(json, bucketName, fileNameWithoutExtension + ".json");
            return $"Text uploaded successfully to s3://{bucketName}/{fileNameWithoutExtension}";
        }
        catch (Exception ex)
        {
            context.Logger.LogLine($"Error uploading text to S3. {ex.Message}");
            return $"Error uploading text to S3. {ex.Message}";
        }
    }
    /// <summary>
    /// check if the file exisits in my-word-count-bucket
    /// </summary>
    /// <param name="bucketName"></param>
    /// <param name="objectKey"></param>
    /// <param name="content"></param>
    /// <returns>is exisit return the content if not exisit return not exisits</returns>
    private async Task<string> DoesObjectExist(string bucketName, string fileName)
    {
        try
        {
            GetObjectRequest request = new GetObjectRequest
            {
                BucketName = bucketName,
                Key = fileName
            };

            GetObjectResponse response = await _s3Client.GetObjectAsync(request);

            using (StreamReader reader = new StreamReader(response.ResponseStream))
            {
                string existingContent = await reader.ReadToEndAsync();
                return existingContent;
            }
        }
        catch (AmazonS3Exception ex)
        {
            if (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return "not exisit"; // Object doesn't exist
            }
            throw; 
        }
    }


    /// <summary>
    /// Get the text of myfile from my-word-count-bucket
    /// </summary>
    /// <param name="bucketName"></param>
    /// <param name="myFile"></param>
    /// <param name="context"></param>
    /// <returns>if myFile exists in my-word-count-bucket return the text </returns>
    private async Task<string> getText(string bucketName, string myFile, ILambdaContext context)
    {
        try
        {
            GetObjectRequest request = new GetObjectRequest
            {
                BucketName = bucketName,
                Key = myFile
            };

            GetObjectResponse response = await _s3Client.GetObjectAsync(request);

            using (StreamReader reader = new StreamReader(response.ResponseStream))
            {
                string existingContent = await reader.ReadToEndAsync();
                return existingContent;
            }
        }
        catch (AmazonS3Exception ex)
        {
            if (ex.StatusCode == HttpStatusCode.NotFound)
            {
                context.Logger.LogLine($"Pleas uplaod the file to my-word-count-bucket");

                throw; // Object doesn't exist
            }
            throw; 
        }
    }
    /// <summary>
    /// Upload file to s3 bucket
    /// </summary>
    /// <param name="text"></param>
    /// <param name="bucketName"></param>
    /// <param name="myFile"></param>
    /// <returns>Task</returns>
    private async Task UploadFileToS3Async(string text, string bucketName, string myFile)
    {
        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = myFile,
            ContentBody = text
        };

        await _s3Client.PutObjectAsync(request);
    }
    /// <summary>
    /// Creates a file containing the number of words in the text and how many times each word appears
    /// </summary>
    /// <param name="text"></param>
    /// <returns> Tth JSON File</returns>
    public string GenerateWordCountJson(string text)
    {
        try
        {
            var wordCount = new Dictionary<string, int>();

            string[] words = text.ToString().Split(new char[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string word in words)
            {
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

            return JsonConvert.SerializeObject(wordCount);
        }
        catch (Exception)
        {
            throw;
        }

    }
}




