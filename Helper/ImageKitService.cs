using Imagekit.Sdk;

public class ImageKitService
{
    private readonly ImagekitClient _client;

    public ImageKitService(IConfiguration config)
    {
        var publicKey = config["ImageKit:PublicKey"];
        var privateKey = config["ImageKit:PrivateKey"];
        var urlEndpoint = config["ImageKit:UrlEndpoint"];

        _client = new ImagekitClient(publicKey, privateKey, urlEndpoint);
    }

    // 🔥 Upload agora retorna URL + FileId
    public async Task<(string url, string fileId)> UploadImage(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        using var ms = new MemoryStream();

        await stream.CopyToAsync(ms);

        var base64 = Convert.ToBase64String(ms.ToArray());

        var request = new FileCreateRequest
        {
            file = base64,
            fileName = file.FileName
        };

        var result = await _client.UploadAsync(request);

        return (result.url, result.fileId);
    }

    // 🔥 Método para deletar imagem
    public async Task DeleteImage(string fileId)
    {
        await _client.DeleteFileAsync(fileId);
    }
}