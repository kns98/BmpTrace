using OpenCvSharp; // For image processing
using PdfSharp.Pdf; // For creating PDFs
using PdfSharp.Drawing; // For drawing on PDFs
using System;
using System.Linq; // For LINQ operations on collections

class Program
{
    static void Main(string[] args)
    {
        // Load the image using OpenCV
        string imagePath = @"c:\tmp\t.bmp";
        Mat image = Cv2.ImRead(imagePath, ImreadModes.Color);

        // Convert the image to grayscale
        Mat gray = new Mat();
        Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);

        // Apply Canny edge detection
        Mat cannyOutput = new Mat();
        Cv2.Canny(gray, cannyOutput, 50, 150);

        // Find contours in the image
        OpenCvSharp.Point[][] contours;
        HierarchyIndex[] hierarchy;
        Cv2.FindContours(
            cannyOutput,
            out contours,
            out hierarchy,
            RetrievalModes.Tree,
            ContourApproximationModes.ApproxSimple
        );

        // Create a new PDF document
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage();
        XGraphics gfx = XGraphics.FromPdfPage(page);

        // Iterate over contours and draw each shape
        foreach (var contour in contours)
        {
            var approx = Cv2.ApproxPolyDP(contour, 0.04 * Cv2.ArcLength(contour, true), true);
            if (approx.Length >= 3) // Draw only if it's a closed shape
            {
                // Convert points for PDF rendering
                var points = approx.Select(p => new XPoint(p.X, p.Y)).ToArray();
                gfx.DrawPolygon(XPens.Black, points);
            }
        }

        // Save the PDF document
        string outputPdfPath = @"c:\tmp\1.pdf";
        document.Save(outputPdfPath);
        document.Close();

        Console.WriteLine("PDF with shapes created successfully!");
    }
}
