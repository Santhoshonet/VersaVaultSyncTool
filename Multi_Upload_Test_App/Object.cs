using System;
using System.Collections.Generic;

namespace Multi_Upload_Test_App
{
    [Serializable]
    public class Object
    {
        public Object()
        {
            Parts = new List<Part>();
        }

        public string UploadId { get; set; }

        public List<Part> Parts { get; set; }
    }

    [Serializable]
    public class Part
    {
        public int PartId { get; set; }

        public string Etag { get; set; }
    }
}