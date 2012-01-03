using System;
using System.Collections.Generic;
using System.IO;
using Amazon.S3;
using Amazon.S3.Model;
using VersaVaultLibrary;

namespace Multi_Upload_Test_App
{
    class Program
    {
        static string _accessKeyId = "";
        static string _secretAccessKey = "";
        // Your AWS Credentials.
        const string ExistingBucketName = "VersaVault";
        const string FilePath = @"C:\Users\Santhosh\Documents\VersaVault\Test.txt";
        private static readonly string KeyName = Path.GetFileName(FilePath);

        static void Main()
        {
            _accessKeyId = Utilities.AwsAccessKey;
            _secretAccessKey = Utilities.AwsSecretKey;

            AmazonS3 s3Client = new AmazonS3Client(_accessKeyId, _secretAccessKey);

            ListMultipartUploadsRequest allMultipartUploadsRequest = new ListMultipartUploadsRequest().WithBucketName(ExistingBucketName);
            ListMultipartUploadsResponse mpUploadsResponse = s3Client.ListMultipartUploads(allMultipartUploadsRequest);

            foreach (MultipartUpload multipartUpload in mpUploadsResponse.MultipartUploads)
            {
            }

            return;

            // List to store upload part responses.
            var uploadResponses = new List<UploadPartResponse>();
            byte[] bytes;
            long contentLength = 0;
            using (var fileStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                contentLength = fileStream.Length;
                bytes = new byte[contentLength];
                fileStream.Read(bytes, 0, Convert.ToInt32(contentLength));
            }
            // 1. Initialize.
            InitiateMultipartUploadRequest initiateRequest = new InitiateMultipartUploadRequest().WithBucketName(ExistingBucketName).WithKey(KeyName);
            InitiateMultipartUploadResponse initResponse = s3Client.InitiateMultipartUpload(initiateRequest);
            try
            {
                // 2. Upload Parts.
                long partSize = 5 * (long)Math.Pow(2, 20); // 5 MB
                long filePosition = 0;
                for (int i = 1; filePosition < contentLength; i++)
                {
                    byte[] bytesToStream;
                    if (filePosition + partSize < contentLength)
                    {
                        bytesToStream = new byte[partSize];
                        Array.Copy(bytes, filePosition, bytesToStream, 0, partSize);
                    }
                    else
                    {
                        bytesToStream = new byte[contentLength - filePosition];
                        Array.Copy(bytes, filePosition, bytesToStream, 0, contentLength - filePosition);
                    }
                    Stream stream = new MemoryStream(bytesToStream);
                    // Create request to upload a part.
                    UploadPartRequest uploadRequest = new UploadPartRequest()
                        .WithBucketName(ExistingBucketName)
                        .WithKey(KeyName)
                        .WithUploadId(initResponse.UploadId)
                        .WithPartNumber(i)
                        .WithPartSize(partSize)
                        .WithFilePosition(filePosition)
                        .WithTimeout(1000000000);
                    uploadRequest.WithInputStream(stream);
                    // Upload part and add response to our list.
                    uploadResponses.Add(s3Client.UploadPart(uploadRequest));
                    filePosition += partSize;
                }
                // Step 3: complete.
                CompleteMultipartUploadRequest completeRequest =
                    new CompleteMultipartUploadRequest()
                    .WithBucketName(ExistingBucketName)
                    .WithKey(KeyName)
                    .WithUploadId(initResponse.UploadId)
                    .WithPartETags(uploadResponses);

                CompleteMultipartUploadResponse completeUploadResponse = s3Client.CompleteMultipartUpload(completeRequest);
                Console.WriteLine(completeUploadResponse.ETag);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Exception occurred: {0}", exception.Message);
                s3Client.AbortMultipartUpload(new AbortMultipartUploadRequest()
                    .WithBucketName(ExistingBucketName)
                    .WithKey(KeyName)
                    .WithUploadId(initResponse.UploadId));
            }
        }

        /*
        static string _accessKeyId = "";
        static string _secretAccessKey = "";

        const string ExistingBucketName = "VersaVault";
        const string KeyName = "1559531169.pdf";
        const string FilePath = @"C:\Users\Santhosh\Documents\VersaVault\1559531169.pdf";

        static void Main()
        {
            _accessKeyId = Utilities.AwsAccessKey;
            _secretAccessKey = Utilities.AwsSecretKey;
            try
            {
                var fileTransferUtility = new TransferUtility(_accessKeyId, _secretAccessKey);
                // 1. Upload a file, file name is used as the object key name.
                fileTransferUtility.Upload(FilePath, ExistingBucketName);
                Console.WriteLine("Upload 1 completed");
                // 2. Specify object key name explicitly.
                fileTransferUtility.Upload(FilePath, ExistingBucketName, KeyName);
                Console.WriteLine("Upload 2 completed");
                // 3. Upload data from a type of System.IO.Stream.
                using (var fileToUpload = new FileStream(FilePath, FileMode.Open, FileAccess.Read))
                {
                    fileTransferUtility.Upload(fileToUpload, ExistingBucketName, KeyName);
                }
                Console.WriteLine("Upload 3 completed");
                // 4.// Specify advanced settings/options.
                TransferUtilityUploadRequest fileTransferUtilityRequest =
                    new TransferUtilityUploadRequest()
                    .WithBucketName(ExistingBucketName)
                    .WithFilePath(FilePath)
                    .WithStorageClass(S3StorageClass.ReducedRedundancy)
                    //.WithMetadata("param1", "Value1")
                    //.WithMetadata("param2", "Value2")
                    .WithPartSize(6291456) // This is 6 MB.
                    .WithKey(KeyName)
                    .WithCannedACL(S3CannedACL.PublicRead);
                fileTransferUtility.Upload(fileTransferUtilityRequest);
                Console.WriteLine("Upload 4 completed");
            }
            catch (AmazonS3Exception s3Exception)
            {
                Console.WriteLine(s3Exception.Message,
                                  s3Exception.InnerException);
            }
        }
         */
    }
}

/*
class Program
{
    static void Main(string[] args)
    {
        int partLength = 1024 * 1024 * 5;
        var partEtags = new List<PartETag>();
        var amazons3 = AWSClientFactory.CreateAmazonS3Client(Utilities.AwsAccessKey, Utilities.AwsSecretKey, new AmazonS3Config { CommunicationProtocol = Protocol.HTTP });
        const string filepath = @"C:\Users\Santhosh\Documents\VersaVault\1559531169.pdf";
        string relativePath = filepath.Replace(Utilities.Path + "\\", "").Replace("\\", "/");
        int partNumber = 1;
        // initiating multi part upload
        var response = amazons3.InitiateMultipartUpload(new InitiateMultipartUploadRequest() { BucketName = "VersaVault", Key = relativePath });
        if (!string.IsNullOrEmpty(response.UploadId))
        {
            var fileLength = new FileInfo(filepath).Length;
            // uploading part
            UploadPartRequest uploadPartREquest;
            UploadPartResponse uploadPartresponse;
            for (int length = 0; length < fileLength; length += partLength)
            {
                uploadPartREquest = new UploadPartRequest
                                            {
                                                BucketName = "VersaVault",
                                                FilePath = filepath,
                                                FilePosition = length,
                                                Key = relativePath,
                                                PartNumber = partNumber,
                                                PartSize = partLength,
                                                UploadId = response.UploadId
                                            };
                uploadPartresponse = null;
                while (uploadPartresponse == null)
                {
                    try
                    {
                        uploadPartresponse = amazons3.UploadPart(uploadPartREquest);
                        partEtags.Add(new PartETag(uploadPartresponse.PartNumber, uploadPartresponse.ETag));
                    }
                    catch (Exception)
                    {
                        uploadPartresponse = null;
                    }
                }
                partNumber += 1;
                if (!string.IsNullOrEmpty(uploadPartresponse.RequestId))
                    continue;
            }

            // uploading last part
            uploadPartREquest = new UploadPartRequest
            {
                BucketName = "VersaVault",
                FilePath = filepath,
                FilePosition = (fileLength / partLength) * partLength,
                Key = relativePath,
                PartNumber = partNumber,
                PartSize = fileLength % partLength,
                UploadId = response.UploadId
            };
            uploadPartresponse = amazons3.UploadPart(uploadPartREquest);
            if (!string.IsNullOrEmpty(uploadPartresponse.RequestId))
            {
                partEtags.Add(new PartETag(uploadPartresponse.PartNumber, uploadPartresponse.ETag));
                // completing upload part
                var completeMultipartUploadResponse = amazons3.CompleteMultipartUpload(new CompleteMultipartUploadRequest
                                                       {
                                                           BucketName = "VersaVault",
                                                           Key = relativePath,
                                                           UploadId = response.UploadId,
                                                           PartETags = partEtags
                                                       });
            }
        }
    }
} */