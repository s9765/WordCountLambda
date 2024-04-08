using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using NUnit.Framework;
using System.Text;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace WordCountLamda;

public class Function
{

    /// <summary>
    /// A simple function that takes a string and does a ToUpper
    /// </summary>
    /// <param name="input">The event for the Lambda function handler to process.</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns></returns>
    ///     private readonly IAmazonS3 _s3Client;
    private readonly IAmazonS3 _s3Client;

    public Function()
    {
        _s3Client = new AmazonS3Client();
    }
    
    public async Task FunctionHandler(string base64String, ILambdaContext context)
    {
        var s=new StringBuilder(base64String);  
        // המרת מחרוזת Base64 לקובץ TXT
        var bytes = Convert.FromBase64String(base64String);
        var fileContent = Encoding.UTF8.GetString(bytes);

        // ספירת מילים
        var wordCount = CountWords(fileContent);

        // יצירת אובייקט JSON עם נתוני ספירת המילים
        var jsonData = CreateJsonData(wordCount);

        // שם קובץ JSON
        var jsonKey = "word-count.json";

        // העלאת אובייקט JSON ל-S3
        await UploadObject(jsonKey, jsonData);
    }

    private Dictionary<string, int> CountWords(string text)
    {
        var words = text.Split(' ', '\n', '\r', '\t', '.', ',', ';');
        var wordCount = new Dictionary<string, int>();

        foreach (var word in words)
        {
            if (string.IsNullOrEmpty(word))
                continue;

            var lowerWord = word.ToLower();
            if (!wordCount.ContainsKey(lowerWord))
                wordCount[lowerWord] = 0;

            wordCount[lowerWord]++;
        }

        return wordCount;
    }

    private string CreateJsonData(Dictionary<string, int> wordCount)
    {
        var jsonData = new StringBuilder();
        jsonData.Append("{");

        foreach (var kvp in wordCount)
        {
            jsonData.Append($"\"{kvp.Key}\": {kvp.Value},");
        }

        jsonData.Remove(jsonData.Length - 1, 1); // הסרת פסיק אחרון
        jsonData.Append("}");

        return jsonData.ToString();
    }

    private async Task UploadObject(string objectKey, string jsonData)
    {
        using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonData)))
        {
            await _s3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = "my-word-count-bucket", // שם ה-bucket שלך
                Key = objectKey,
                ContentType = "application/json",
                ContentBody = memoryStream.ToString()
            }) ;
        }
    }
    //[Test]
    //public void test1()
    //{
    //    FunctionHandler("jhgkj gh gh gh");

    //}
}