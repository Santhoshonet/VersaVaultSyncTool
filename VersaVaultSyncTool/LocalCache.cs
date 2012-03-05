using System;
using System.Collections.Generic;
using Amazon.S3.Model;

namespace VersaVaultSyncTool
{
    [Serializable]
    public class ObjectInfo
    {
        public ObjectInfo()
        {
            UploadPartResponses = new List<UploadPartResponse>();
            LastModified = DateTime.MinValue;
        }

        public string RelativePath { get; set; }

        public string Bucketkey { get; set; }

        public string UploadId { get; set; }

        public DateTime LastModified { get; set; }

        public List<UploadPartResponse> UploadPartResponses { get; set; }
    }
}