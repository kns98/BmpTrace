using OpenCvSharp;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using Svg;
using System;
using System.IO;
using System.Linq; // For LINQ operations on collections

class Program
{
    static void Main(string[] args)
    {
        // Default values
        string imagePath = null;
        double lowThreshold = 50.0;
        double highThreshold = 150.0;
        string outputPdfPath = null;
        string outputSvgPath = null;
        string retrievalMode = "Tree";
        string approximationMode = "Simple";
        double epsilonFactor = 0.04;

        // Parse command-line arguments
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--image-path":
                    imagePath = args[++i];
                    break;
                case "--low-threshold":
                    lowThreshold = double.Parse(args[++i]);
                    break;
                case "--high-threshold":
                    highThreshold = double.Parse(args[++i]);
                    break;
                case "--output-pdf-path":
                    outputPdfPath = args[++i];
                    break;
                case "--output-svg-path":
                    outputSvgPath = args[++i];
                    break;
                case "--retrieval-mode":
                    retrievalMode = args[++i];
                    break;
                case "--approximation-mode":
                    approximationMode = args[++i];
                    break;
                case "--epsilon-factor":
                    epsilonFactor = double.Parse(args[++i]);
                    break;
                default:
                    Console.WriteLine($"Unknown option: {args[i]}");
                    break;
            }
        }

        // Check required arguments
        if (string.IsNullOrEmpty(imagePath) || string.IsNullOrEmpty(outputPdfPath) || string.IsNullOrEmpty(outputSvgPath))
        {
            Console.WriteLine("Usage: program --image-path <path> --output-pdf-path <path> --output-svg-path <path> [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  --low-threshold <value>        Low threshold for Canny edge detection (default: 50)");
            Console.WriteLine("  --high-threshold <value>       High threshold for Canny edge detection (default: 150)");
            Console.WriteLine("  --retrieval-mode <mode>        Contour retrieval mode (default: Tree)");
            Console.WriteLine("  --approximation-mode <mode>    Contour approximation mode (default: Simple)");
            Console.WriteLine("  --epsilon-factor <value>       Epsilon factor for contour approximation (default: 0.04)");
            return;
        }

        // Run image processing and document generation
        ProcessImage(imagePath, lowThreshold, highThreshold, outputPdfPath, outputSvgPath, retrievalMode, approximationMode, epsilonFactor);
    }

    static void ProcessImage(string imagePath, double lowThreshold, double highThreshold, string outputPdfPath, string outputSvgPath, string retrievalMode, string approximationMode, double epsilonFactor)
    {
        // Load the image using OpenCV
        Mat image = Cv2.ImRead(imagePath, ImreadModes.Color);

        // Convert the image to grayscale
        Mat gray = new Mat();
        Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);

        // Apply Canny edge detection
        Mat cannyOutput = new Mat();
        Cv2.Canny(gray, cannyOutput, lowThreshold, highThreshold);

        // Map the retrieval mode
        RetrievalModes mappedRetrievalMode = MapRetrievalMode(retrievalMode);

        // Map the approximation mode
        ContourApproximationModes mappedApproximationMode = MapApproximationMode(approximationMode);

        // Find contours in the image
        OpenCvSharp.Point[][] contours;
        HierarchyIndex[] hierarchy;
        Cv2.FindContours(
            cannyOutput,
            out contours,
            out hierarchy,
            mappedRetrievalMode,
            mappedApproximationMode
        );

        // Create a new PDF document
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage();
        XGraphics gfx = XGraphics.FromPdfPage(page);

        // Initialize SVG content
        var svgContent = new System.Text.StringBuilder();
        svgContent.AppendLine($"<svg width=\"{image.Width}\" height=\"{image.Height}\" xmlns=\"http://www.w3.org/2000/svg\">");

        // Iterate over contours and draw each shape
        foreach (var contour in contours)
        {
            double epsilon = epsilonFactor * Cv2.ArcLength(contour, true);
            var approx = Cv2.ApproxPolyDP(contour, epsilon, true);

            if (approx.Length >= 3) // Draw only if it's a closed shape
            {
                // Convert points for PDF rendering
                var pdfPoints = approx.Select(p => new XPoint(p.X, p.Y)).ToArray();
                gfx.DrawPolygon(XPens.Black, pdfPoints);

                // Convert points to SVG path
                var points = string.Join(" ", approx.Select(p => $"{p.X},{p.Y}"));
                svgContent.AppendLine($"<polygon points=\"{points}\" style=\"fill:none;stroke:black;stroke-width:1\" />");
            }
        }

        svgContent.AppendLine("</svg>");

        // Save the PDF document
        document.Save(outputPdfPath);
        document.Close();

        // Save the SVG content to a file
        File.WriteAllText(outputSvgPath, svgContent.ToString());

        Console.WriteLine("PDF and SVG with shapes created successfully!");
    }

    static RetrievalModes MapRetrievalMode(string mode)
    {
        return mode switch
        {
            "External" => RetrievalModes.External,
            "List" => RetrievalModes.List,
            "CComp" => RetrievalModes.CComp,
            "Tree" => RetrievalModes.Tree,
            "FloodFill" => RetrievalModes.FloodFill,
            _ => throw new ArgumentException("Invalid retrieval mode"),
        };
    }

    static ContourApproximationModes MapApproximationMode(string mode)
    {
        return mode switch
        {
            "Simple" => ContourApproximationModes.ApproxSimple,
            "TC89L1" => ContourApproximationModes.ApproxTC89L1,
            "TC89KCOS" => ContourApproximationModes.ApproxTC89KCOS,
            _ => throw new ArgumentException("Invalid approximation mode"),
        };
    }
}
