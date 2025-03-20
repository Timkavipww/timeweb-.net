var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.Configure<S3Options>(builder.Configuration.GetSection("Aws"));
builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var options = sp.GetRequiredService<IOptions<S3Options>>().Value;

    return new AmazonS3Client(options.AccessKey, options.SecretAccessKey, new AmazonS3Config
    {
        ServiceURL = "https://s3.timeweb.cloud",
        ForcePathStyle = true
    });
});
builder.Services.AddSwaggerGen();
var app = builder.Build();
app.MapScalarApiReference();
app.MapOpenApi();
app.UseRouting();
app.UseSwagger();
app.UseSwaggerUI();

app.MapPut("buckets", async (IAmazonS3 _s3,  string bucketName) =>
{
     var putBucketRequest = new PutBucketRequest
            {
                BucketName = bucketName,
                UseClientRegion = true
            };
    await _s3.PutBucketAsync(putBucketRequest);
});

app.MapGet("buckets", async (IAmazonS3 _s3) => 
{
    Console.WriteLine("Список бакетов");
    var response = await _s3.ListBucketsAsync();
    return response.Buckets.Select(x => x.BucketName);

});

app.MapPost("files/{id}", async (
    IAmazonS3 _s3, 
    IOptions<S3Options> _s3Options, 
    [FromRoute] int id, 
    [FromForm] IFormFile file) =>
{
    Console.WriteLine("Загрузка файла");
    var request = new PutObjectRequest
    {
        BucketName = _s3Options.Value.BucketName,
        Key = $"files/{id}",
        ContentType = file.ContentType,
        InputStream = file.OpenReadStream(),    
    };

    var response = await _s3.PutObjectAsync(request);
    return Results.Ok(response);
}).DisableAntiforgery().WithOpenApi(operation => OpenApiConfiguration.ConfigureUploadImageOperation());

app.MapGet("files", async (
    IAmazonS3 _s3,
    string bucketName
) =>
{
    var request = new ListObjectsV2Request
    {
        BucketName = bucketName
    };
    var response = await _s3.ListObjectsV2Async(request);
    return response.S3Objects.ToArray().Select(a => a.Key);
});

app.MapGet("files/{id}", async (
    IAmazonS3 _s3, 
    IOptions<S3Options> _s3Options, 
    [FromRoute] int id) => 
{   
    Console.WriteLine("Получение файла");
    var objectRequest = new GetObjectRequest
    {
        BucketName = _s3Options.Value.BucketName,
        Key = $"files/{id}"
    };
    
    var response = await _s3.GetObjectAsync(objectRequest);
    if (response.HttpStatusCode == HttpStatusCode.OK)
        return Results.File(response.ResponseStream, response.Headers["Content-Type"]);

    return Results.BadRequest();
});
app.MapDelete("files/{id}", async (
    IAmazonS3 _s3,
    string bucketName,
    string id
) => 
{
    var request = new DeleteObjectRequest
    {
        BucketName = bucketName,
        Key = id
    };

    var response = await _s3.DeleteObjectAsync(bucketName, id);
    return response;
});

app.Run();
