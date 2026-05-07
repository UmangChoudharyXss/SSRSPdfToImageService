using Microsoft.Data.SqlClient;
using Syncfusion.PdfToImageConverter;
using System.IO;

public class PdfToImageserviceNewOld
{
    private readonly string _connectionString = "";
    public void ConvertFolderWithStructure(string inputBasePath, string outputRootPath)
    {
        // Extract DB Name (0103)
        string dbName = new DirectoryInfo(inputBasePath).Parent?.Name ?? "UNKNOWN";

        var purchaseFolders = Directory.GetDirectories(inputBasePath);

        foreach (var purchaseFolder in purchaseFolders)
        {
            try
            {
                string purchaseId = Path.GetFileName(purchaseFolder);

                var guidFolders = Directory.GetDirectories(purchaseFolder);

                foreach (var guidFolder in guidFolders)
                {
                    var pdfFiles = Directory.GetFiles(guidFolder, "*.pdf");

                    foreach (var pdfFile in pdfFiles)
                    {
                        try
                        {
                            string targetDirectory = Path.Combine(
                                outputRootPath,
                                dbName,
                                "Purchases",
                                purchaseId
                            );

                            Directory.CreateDirectory(targetDirectory);

                            ConvertPdf(pdfFile, targetDirectory);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ PDF error: {pdfFile} -> {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Folder error: {purchaseFolder} -> {ex.Message}");
            }
        }
    }


    //public void ConvertFolderWithStructure(string inputBasePath, string outputBasePath)
    //{

    //    var pdfFiles = Directory.GetFiles(inputBasePath, "*.pdf", SearchOption.AllDirectories);

    //    foreach (var pdfFile in pdfFiles)
    //    {
    //        try
    //        {
    //            var relativePath = Path.GetRelativePath(inputBasePath, pdfFile);
    //            var relativeDirectory = Path.GetDirectoryName(relativePath);

    //            var targetDirectory = Path.Combine(outputBasePath, relativeDirectory ?? "");

    //            if (!Directory.Exists(targetDirectory))
    //                Directory.CreateDirectory(targetDirectory);

    //            ConvertPdf(pdfFile, targetDirectory);
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine($"Error processing {pdfFile}: {ex.Message}");
    //        }
    //    }
    //}

    private static void ConvertPdf(string pdfPath, string outputFolder)
    {
        try
        {
            PdfToImageConverter imageConverter = new PdfToImageConverter();

            string fileName = Path.GetFileNameWithoutExtension(pdfPath);
            DateTime pdfLastWrite = File.GetLastWriteTimeUtc(pdfPath);

            var existingImages = Directory.GetFiles(outputFolder, $"{fileName}_Page_*.png");

            if (existingImages.Length > 0)
            {
                DateTime latestImageTime = existingImages
                    .Select(f => File.GetLastWriteTimeUtc(f))
                    .Max();

                if (latestImageTime >= pdfLastWrite)
                {
                    Console.WriteLine($"Skipped (no change): {pdfPath}");
                    return;
                }

                foreach (var img in existingImages)
                    File.Delete(img);
            }
            using (FileStream inputStream = new FileStream(pdfPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                imageConverter.Load(inputStream);

                int pageCount = imageConverter.PageCount;

                for (int i = 0; i < pageCount; i++)
                {
                    try
                    {
                        using Stream outputStream = imageConverter.Convert(i, false, false);

                        string outputPath = Path.Combine(outputFolder, $"{fileName}_Page_{i + 1}.png");

                        if (File.Exists(outputPath))
                            continue;

                        if (outputStream.Length == 0)
                        {
                            Console.WriteLine($"Empty output for {pdfPath} Page {i + 1}");
                            continue;
                        }

                        outputStream.Position = 0;

                        using FileStream fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
                        outputStream.CopyTo(fileStream);

                        Console.WriteLine($"Converted: {outputPath}");

                        //To insert into database
                        //InsertIntoDatabase(connectionString,fileName,outputPath,i + 1);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Page {i + 1} failed for {pdfPath}: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed PDF: {pdfPath} -> {ex.Message}");
        }
    }
    private static void InsertIntoDatabase(string connectionString, string pdfName, string imagePath, int pageNumber)
    {
        using (SqlConnection con = new SqlConnection(connectionString))
        {
            string query = @"INSERT INTO PdfConvertedImages (PdfFileName, ImagePath, PageNumber)
                         VALUES (@PdfFileName, @ImagePath, @PageNumber)";

            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@PdfFileName", pdfName);
                cmd.Parameters.AddWithValue("@ImagePath", imagePath);
                cmd.Parameters.AddWithValue("@PageNumber", pageNumber);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}