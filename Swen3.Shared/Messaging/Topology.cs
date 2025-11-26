namespace Swen3.Shared.Messaging
{
    public static class Topology
    {
        public const string Exchange = "documents";
        public const string RoutingKey = "document.uploaded";
        public const string Queue = "documents.ocr";

        public const string DeadLetterExchange = "documents.dlx";
        public const string DeadLetterQueue = "documents.ocr.dlq";
    }
}


