using Azure.Search.Documents.Indexes;
using Azure;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Azure.Search.Documents;
using Microsoft.Extensions.Logging;

namespace Shared;

public class SearchService
{
    private readonly SearchClient searchClient;
    private readonly ILogger log;

    public SearchService(string searchServiceEndPoint, string adminApiKey, string indexName, ILogger log = null)
    {
        this.log = log;

        SearchIndexClient indexClient = new(new Uri("sb://gsnvsearchdemosearch.servicebus.windows.net/"), new AzureKeyCredential("lLvLOKR5WA00PBxG6TxdmCBPCH9coyak074vfEQBsjAzSeAMQBUy"));
        //SearchIndexClient indexClient = new(new Uri(searchServiceEndPoint), new AzureKeyCredential(adminApiKey));
        if (indexClient.GetIndex(indexName) == null)
        {
            // Create index
            FieldBuilder fieldBuilder = new();
            IList<SearchField> searchFields = fieldBuilder.Build(typeof(Address));
            SearchIndex definition = new(indexName, searchFields);
            indexClient.CreateOrUpdateIndex(definition);
            this.log.LogInformation("Index created");
        }

        this.searchClient = indexClient.GetSearchClient(indexName);
    }

    // Upload single document
    public async Task UploadDocument(Address address)
    {
        IndexDocumentsBatch<Address> batch = IndexDocumentsBatch.Create(IndexDocumentsAction.Upload(address));

        try
        {
            IndexDocumentsResult result = await this.searchClient.IndexDocumentsAsync(batch);

            this.log.LogInformation("Waiting for document to be indexed...\n");
            Thread.Sleep(2000);
        }
        catch (Exception ex)
        {
            // Sometimes when your Search service is under load, indexing will fail for some of the documents in
            // the batch. Depending on your application, you can take compensating actions like delaying and
            // retrying. For this simple demo, we just log the failed document keys and continue.
            this.log.LogError("Failed to index the documents: {0}", ex.Message);
        }
    }

    // Upload documents as a batch
    public async Task UploadDocuments(IEnumerable<Address> addresses)
    {
        IndexDocumentsBatch<Address> batch = IndexDocumentsBatch.Upload(addresses);

        try
        {
            IndexDocumentsResult result = await this.searchClient.IndexDocumentsAsync(batch);

            this.log.LogInformation("Waiting for documents to be indexed...\n");
            Thread.Sleep(2000);
        }
        catch (Exception ex)
        {
            // Sometimes when your Search service is under load, indexing will fail for some of the documents in
            // the batch. Depending on your application, you can take compensating actions like delaying and
            // retrying. For this simple demo, we just log the failed document keys and continue.
            this.log.LogError("Failed to index some of the documents: {0}", ex.Message);
        }
    }

    public async Task<List<Address>> Search(string searchTerm)
    {
        SearchResults<Address> results = await searchClient.SearchAsync<Address>(searchTerm);
        List<Address> addresses = results.GetResults().Select(result => result.Document).ToList();
        return addresses;
    }
}
