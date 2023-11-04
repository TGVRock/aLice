using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using CatSdk.Facade;
using CatSdk.Symbol;

namespace aLice.Services;

public class Symbol
{
    private readonly string Node;
    public Symbol(string node)
    {
        Node = node;
    }
    
    public async Task<bool> CheckNodeHealth()
    {
        using var client = new HttpClient();
        var response = await client.GetAsync(Node + "/node/health");
        if (!response.IsSuccessStatusCode) return false;
        var text = await response.Content.ReadAsStringAsync();
        var json = JsonNode.Parse(text);
        var status = json?["status"];
        if (status == null) return false;
        var apiNode = status["apiNode"];
        var db = status["db"];
        return apiNode?.ToString() == "up" && db?.ToString() == "up";
    }

    public async Task<string> CheckStatus(string hash)
    {
        using var client = new HttpClient();
        var response = await client.GetAsync(Node + "/transactionStatus/" + hash);
        Console.WriteLine(Node + "/transactionStatus/" + hash);
        if (!response.IsSuccessStatusCode) throw new Exception("Transaction not found");
        var text = await response.Content.ReadAsStringAsync();
        var json = JsonNode.Parse(text);
        if (json != null) return (string) json["code"];
        throw new Exception("json is not correct format");
    }
    
    public async Task<string> Announce(string payload)
    {
        using var client = new HttpClient();
        var content = new StringContent("{\"payload\": \"" + payload + "\"}", Encoding.UTF8, "application/json");
        var response =  client.PutAsync(Node + "/transactions", content).Result;
        return await response.Content.ReadAsStringAsync();
    }

    public static (string hash, string address) GetHash(string payload)
    {
        var tx = TransactionFactory.Deserialize(payload);
        var facade = new SymbolFacade(tx.Network == NetworkType.MAINNET ? CatSdk.Symbol.Network.MainNet : CatSdk.Symbol.Network.TestNet);
        return (facade.HashTransaction(tx).ToString(), facade.Network.PublicKeyToAddress(tx.SignerPublicKey).ToString());
    }
}