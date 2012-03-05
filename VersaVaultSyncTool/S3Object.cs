using System;

namespace VersaVaultSyncTool
{
    public class S3Object
    {
        public string FileName { get; set; }

        public bool Folder { get; set; }

        public string Key { get; set; }

        public DateTime LastModified { get; set; }

        public string Uid { get; set; }

        public bool Status { get; set; }

        public bool Shared { get; set; }

        public String Username { get; set; }
    }

    public class s3_object
    {
        public S3Object[] S3Object { get; set; }
    }
}