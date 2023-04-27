using Azure.Search.Documents.Indexes;
using Azure;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Azure.Search.Documents;

namespace Shared;

public class SearchService
{
    private readonly SearchClient searchClient;

    public SearchService(string searchServiceEndPoint, string adminApiKey, string indexName)
    {
        SearchIndexClient indexClient = new(new Uri(searchServiceEndPoint), new AzureKeyCredential(adminApiKey));
        bool exists = false;
        try
        {
            Response<SearchIndex>? existingIndex = indexClient.GetIndex(indexName);
            if (existingIndex != null)
            {
                exists = true;
            }
        }
        catch (Exception)
        {
            exists = false;
        }

        if (!exists)
        {
            // Create index
            FieldBuilder fieldBuilder = new();
            IList<SearchField> searchFields = fieldBuilder.Build(typeof(Address));
            SearchIndex definition = new(indexName, searchFields);
            indexClient.CreateOrUpdateIndex(definition);
            Console.WriteLine("Index created");
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

            Console.WriteLine("Waiting for document to be indexed...\n");
            Thread.Sleep(2000);
        }
        catch (Exception ex)
        {
            // Sometimes when your Search service is under load, indexing will fail for some of the documents in
            // the batch. Depending on your application, you can take compensating actions like delaying and
            // retrying. For this simple demo, we just log the failed document keys and continue.
            Console.WriteLine("Failed to index the documents: {0}", ex.Message);
        }
    }

    // Upload documents as a batch
    public async Task UploadDocuments(IEnumerable<Address> addresses)
    {
        IndexDocumentsBatch<Address> batch = IndexDocumentsBatch.Upload(addresses);

        try
        {
            IndexDocumentsResult result = await this.searchClient.IndexDocumentsAsync(batch);

            Console.WriteLine("Waiting for documents to be indexed...\n");
            Thread.Sleep(2000);
        }
        catch (Exception ex)
        {
            // Sometimes when your Search service is under load, indexing will fail for some of the documents in
            // the batch. Depending on your application, you can take compensating actions like delaying and
            // retrying. For this simple demo, we just log the failed document keys and continue.
            Console.WriteLine("Failed to index some of the documents: {0}", ex.Message);
        }
    }

    public async Task<List<Address>> Search(string searchTerm)
    {
        SearchOptions options = new()
        {
            QueryType = SearchQueryType.Full
        };
        SearchResults<Address> results = await this.searchClient.SearchAsync<Address>($"{searchTerm}*", options);
        List<Address> addresses = results.GetResults().Select(result => result.Document).ToList();
        return addresses;
    }
}
