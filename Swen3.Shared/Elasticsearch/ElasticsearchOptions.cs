using System.ComponentModel.DataAnnotations;

namespace Swen3.Shared.Elasticsearch;

public class ElasticsearchOptions
{
    [Required]
    public string Url { get; set; } = "http://elasticsearch:9200";

    public string IndexName { get; set; } = "documents";
}

