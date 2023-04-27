using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

namespace Shared;

public class Address
{
    public Address()
    {
    }

    public Address(string id)
    {
        this.Id = id;
    }

    [SimpleField(IsKey = true, IsFilterable = true)]
    public string Id { get; set; } = string.Empty;

    [SearchableField(IsFilterable = true, IsSortable = true, AnalyzerName = LexicalAnalyzerName.Values.EnLucene)]
    public string Line1 { get; set; } = string.Empty;

    [SearchableField(IsFilterable = true, IsSortable = true)]
    public string Line2 { get; set; } = string.Empty;

    [SearchableField(IsFilterable = true, IsSortable = true, AnalyzerName = LexicalAnalyzerName.Values.EnLucene)]
    public string City { get; set; } = string.Empty;

    [SearchableField(IsFilterable = true, IsSortable = true, AnalyzerName = LexicalAnalyzerName.Values.EnLucene)]
    public string State { get; set; } = string.Empty;
    
    [SearchableField(IsFilterable = true, IsSortable = true)]
    public string PostalCode { get; set; } = string.Empty;
}
