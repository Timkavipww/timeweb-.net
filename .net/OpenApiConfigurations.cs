namespace timeweb;

public static class OpenApiConfiguration
{
    public static OpenApiOperation ConfigureUploadImageOperation()
    {
        return new OpenApiOperation
        {
            Summary = "Upload an image to S3",
            Parameters = new List<OpenApiParameter>
            {
                new OpenApiParameter
                {
                    Name = "id",
                    In = ParameterLocation.Path,
                    Required = true,
                    Schema = new OpenApiSchema { Type = "integer", Format = "int32" }
                }
            },
            RequestBody = new OpenApiRequestBody
            {
                Content =
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties =
                            {
                                ["file"] = new OpenApiSchema
                                {
                                    Type = "string",
                                    Format = "binary"
                                }
                            }
                        }
                    }
                }
            }
        };
    }
}
