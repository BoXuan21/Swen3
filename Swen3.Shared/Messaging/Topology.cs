namespace Swen3.Shared.Messaging
{
    public static class Topology
    {
        public const string Exchange = "documents";
        public const string RoutingKey = "document.uploaded";
        public const string Queue = "documents.ocr";

        public const string DeadLetterExchange = "documents.dlx";
        public const string DeadLetterQueue = "documents.ocr.dlq";

        public const string ResultQueue = "ocr.results";
        public const string ResultExchange = "ocr";
        public const string ResultRoutingKey = "ocr.read";

        public const string ResultDLX = "ocr.dlx";
        public const string ResultDLQ = "ocr.results.dlq";
    }
}


