namespace ComputerVision
{
    using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
    using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
    using Spectre.Console;
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;


    // Labb 2 - Bildtjänster i Azure AI, Emma Lind, .NET23
    class Program
    {
        // Computer Vision API-key and endpoint-URL
        private const string subscriptionKey = "257224d75fc5459b95eb4448acc55b55";
        private const string endpoint = "https://computervisionlindemma.cognitiveservices.azure.com/";

        static async Task Main(string[] args)
        {
            // ComputerVisionClient-instance
            ComputerVisionClient client = new ComputerVisionClient(new ApiKeyServiceClientCredentials(subscriptionKey))
            {
                Endpoint = endpoint
            };

            Console.WriteLine("Enter a local image path or URL:");
            string imagePath = Console.ReadLine();

            // determines if imagePath input is an URL or local path
            if (Uri.IsWellFormedUriString(imagePath, UriKind.Absolute))
            {
                await AnalyzeImageUrl(client, imagePath);
            }
            else if (File.Exists(imagePath))
            {
                await AnalyzeImageLocal(client, imagePath);
            }
            else
            {
                Console.WriteLine("Invalid path or URL. Try again.");
            }
        }


        // ------------- ANALYZE IMAGES -----------------------------------------------

        // Analyzes image from a url
        private static async Task AnalyzeImageUrl(ComputerVisionClient client, string imageUrl)
        {
            AnsiConsole.MarkupLine("[cyan]Analyzing image from URL...[/]");

            // Analyze image with description, tags and object positions
            ImageAnalysis analysis = await client.AnalyzeImageAsync(imageUrl, new List<VisualFeatureTypes?>
        {
            VisualFeatureTypes.Description,
            VisualFeatureTypes.Tags,
            VisualFeatureTypes.Objects
        });

            DisplayResults(analysis);
            await GenerateThumbnail(client, imageUrl, true);
        }

        // Analyzes a local image
        private static async Task AnalyzeImageLocal(ComputerVisionClient client, string imagePath)
        {
            Console.WriteLine("Analyzing local image...\n");

            using (Stream imageStream = File.OpenRead(imagePath))
            {
                // Analyze image with description, tags and object positions
                ImageAnalysis analysis = await client.AnalyzeImageInStreamAsync(imageStream, new List<VisualFeatureTypes?>
            {
                VisualFeatureTypes.Description,
                VisualFeatureTypes.Tags,
                VisualFeatureTypes.Objects
            });

                DisplayResults(analysis);
                await GenerateThumbnail(client, imagePath, false);
            }
        }


        // --------------- GENERATE THUMBNAIL -----------------------------------------
        private static async Task GenerateThumbnail(ComputerVisionClient client, string imagePathOrUrl, bool isUrl)
        {
            // lets user choose size of thumbnail (max 1024 and min 25)
            Console.WriteLine("\nEnter your preferred sizing for the thumbnail (eg. h:100, w:100)");

            int height;
            Console.Write("Height: ");
            while (!int.TryParse(Console.ReadLine(), out height) || height > 1024 || height < 25)
            {
                Console.WriteLine("Please write an integer input(max 1024, min 25): ");
            }
            int width;
            Console.Write("width: ");
            while (!int.TryParse(Console.ReadLine(), out width) || width > 1024 || width < 25)
            {
                Console.WriteLine("Please write an integer input (max 1024, min 25): ");
            }

            AnsiConsole.MarkupLine("\n[cyan]Generating thumbnail...[/]");

            Stream thumbnailStream;
            if (isUrl)
            {
                // Generates thumbnail from URL
                thumbnailStream = await client.GenerateThumbnailAsync(width, height, imagePathOrUrl, smartCropping: true);
            }
            else
            {
                // Generates thumbnail from local image
                using (Stream imageStream = File.OpenRead(imagePathOrUrl))
                {
                    thumbnailStream = await client.GenerateThumbnailInStreamAsync(width, height, imageStream, smartCropping: true);
                }
            }

            //  gets current project directory as string
            string projectDirectory = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;

            // path to Thumbnails folder in the project
            string thumbnailsDirectory = Path.Combine(projectDirectory, "Thumbnails");

            // if Thumbnails folder does not exist, it is created
            if (!Directory.Exists(thumbnailsDirectory))
            {
                Directory.CreateDirectory(thumbnailsDirectory);
            }

            string uniqueId = Guid.NewGuid().ToString(); //unique GUID in order to save several thumbnails with different names
            string thumbnailType = isUrl ? "url" : "local"; // adds url or local to the file name
            string thumbnailPath = Path.Combine(thumbnailsDirectory, $"{thumbnailType}_thumbnail_{uniqueId}.jpg"); // path to the new thumbnail

            // Saves the thumbnail image
            using (FileStream fs = new FileStream(thumbnailPath, FileMode.Create, FileAccess.Write))
            {
                await thumbnailStream.CopyToAsync(fs);
            }

            AnsiConsole.MarkupLine($"Thumbnail created and saved as: {thumbnailPath}\n[green]Have a great day![/]");
        }


        // ------------ SHOWS RESULTS FROM ANALYSIS ------------------------------------------------
        private static void DisplayResults(ImageAnalysis analysis)
        {
            AnsiConsole.MarkupLine("\n[yellow]Description:[/] " + string.Join(", ", analysis.Description.Captions.Select(c => c.Text)));
            AnsiConsole.MarkupLine("\n[blue]Tags:[/] " + string.Join(", ", analysis.Tags.Select(t => t.Name)));

            // describes each of the objects identified and their positions
            if (analysis.Objects.Count > 0)
            {
                AnsiConsole.MarkupLine("\n[magenta]Identified objects:[/]");
                foreach (var obj in analysis.Objects)
                {
                    Console.WriteLine($" - {obj.ObjectProperty} at position {obj.Rectangle.X}, {obj.Rectangle.Y}, width {obj.Rectangle.W}, height {obj.Rectangle.H}");
                }
            }
            else
            {
                Console.WriteLine("No objects identified.");
            }
        }
    }
}
