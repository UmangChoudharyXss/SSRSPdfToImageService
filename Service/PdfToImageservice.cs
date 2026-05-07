using Microsoft.Data.SqlClient;
using SSRSPdfToImageService.Service.IOParams;
using Syncfusion.PdfToImageConverter;

public class PdfToImageService 
{
    //test
    //private readonly string _connectionString = "Server=192.168.222.5\\Test;Database=master;User Id=sa;Password=xcv**123;MultipleActiveResultSets=True;TrustServerCertificate=True;";
    private readonly string _connectionString = "Server=10.10.1.34;Database=master;User Id=sa;Password=xcv**123;MultipleActiveResultSets=True;TrustServerCertificate=True;";
    public void ProcessAllDatabases(string officeRoot = @"\\fs1\OFFICE", string outputRoot = @"\\fs1\CertificateData\ALL\PDFtoImage")
    {
        //FOR ALL
        //var dbFolders = Directory.GetDirectories(officeRoot);
        var dbFolders = Directory.GetDirectories(officeRoot)
                         .Where(x => x.Contains(@"\\fs1\OFFICE\1301")) //Make in command line
                         .ToArray();
        foreach (var dbFolder in dbFolders)
        {
            try
            {
                string dbName = Path.GetFileName(dbFolder);

                string purchasesPath = Path.Combine(dbFolder, "DiaAdminFiles", "Purchases");

                if (!Directory.Exists(purchasesPath))
                    continue;

                Console.WriteLine($"Processing DB: {dbName}");

                var purchaseFolders = Directory.GetDirectories(purchasesPath);

                foreach (var purchaseFolder in purchaseFolders)
                {
                    string TransactionId = Path.GetFileName(purchaseFolder);

                    var guidFolders = Directory.GetDirectories(purchaseFolder);

                    // Optional: latest only
                    // var guidFolders = Directory.GetDirectories(purchaseFolder)
                    //     .OrderByDescending(f => Directory.GetLastWriteTimeUtc(f))
                    //     .Take(1);

                    foreach (var guidFolder in guidFolders)
                    {
                        var pdfFiles = Directory.GetFiles(guidFolder, "*.pdf");

                        foreach (var pdfFile in pdfFiles)
                        {
                            try
                            {
                                string targetDirectory = Path.Combine(
                                    outputRoot,
                                    dbName
                                );

                                Directory.CreateDirectory(targetDirectory);

                                ConvertPdfCustom(pdfFile, targetDirectory, dbName, TransactionId);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"PDF error: {pdfFile} -> {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB error: {dbFolder} -> {ex.Message}");
            }
        }
    }
    private void ConvertPdfCustom(string pdfPath, string outputFolder, string dbName, string TransactionId)
    {
        try
        {
            string fileName = Path.GetFileNameWithoutExtension(pdfPath);
            DateTime pdfLastWrite = File.GetLastWriteTimeUtc(pdfPath);

            var existingImages = Directory.GetFiles(outputFolder, $"{dbName}-PDFtoImage-{TransactionId}_{fileName}_Page_*.png");

            // Skip unchanged
            if (existingImages.Length > 0)
            {
                DateTime latestImage = existingImages.Max(f => File.GetLastWriteTimeUtc(f));

                if (latestImage >= pdfLastWrite)
                {
                    Console.WriteLine($"Skipped: {pdfPath}");
                    return;
                }

                foreach (var img in existingImages)
                    File.Delete(img);
            }

            Console.WriteLine($"Processing: {pdfPath}");

            PdfToImageConverter converter = new PdfToImageConverter();

            using (FileStream inputStream = new FileStream(pdfPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                converter.Load(inputStream);

                int pageCount = converter.PageCount;

                for (int i = 0; i < pageCount; i++)
                {
                    try
                    {
                        using Stream outputStream = converter.Convert(i, false, false);

                        if (outputStream.Length == 0)
                            continue;

                        outputStream.Position = 0;
                        var filename = $"{dbName}-PDFtoImage-{TransactionId}_{fileName}_Page_{i + 1}.png";
                        string outputFile = Path.Combine(
                            outputFolder,
                           filename
                        );

                        using FileStream fs = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
                        outputStream.CopyTo(fs);
                        var filepath = $"HTTPS://certificates.gemapp.be/View/PDFtoImage/{dbName}/{filename}";
                        Console.WriteLine($"{outputFile}");
                        InsertUpdateImageRecord(_connectionString, dbName, int.Parse(TransactionId), filename, i + 1, filepath);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Page error: {pdfPath} Page {i + 1} -> {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed: {pdfPath} -> {ex.Message}");
        }
    }
    private void InsertUpdateImageRecord(string connectionString, string dbName, int TransactionId, string pdfName, int pageNo, string imagePath)
    {
        using var con = new SqlConnection(connectionString);

        string sql = $@"
                        UPDATE XSS_{dbName}_ALL_MSDESQL.dbo.PdfConvertedImages
                        SET ImagePath = @ImagePath,CreatedOn = SYSDATETIME()
                        WHERE DatabaseName = @DatabaseName
                          AND TransactionId   = @TransactionId
                          AND TransactionTypeId   = 1
                          AND PdfFileName  = @PdfFileName
                          AND PageNumber   = @PageNumber

                        IF @@ROWCOUNT = 0
                        BEGIN
                            INSERT INTO XSS_{dbName}_ALL_MSDESQL.dbo.PdfConvertedImages
                            (DatabaseName,TransactionId,TransactionTypeId,PdfFileName,PageNumber,ImagePath,CreatedOn)
                            VALUES
                            (@DatabaseName,@TransactionId,1,@PdfFileName,@PageNumber,@ImagePath,SYSDATETIME())
                        END";

        using var cmd = new SqlCommand(sql, con);
        cmd.Parameters.AddWithValue("@DatabaseName", dbName);
        cmd.Parameters.AddWithValue("@TransactionId", TransactionId);
        cmd.Parameters.AddWithValue("@PdfFileName", pdfName);
        cmd.Parameters.AddWithValue("@PageNumber", pageNo);
        cmd.Parameters.AddWithValue("@ImagePath", imagePath);

        con.Open();
        cmd.ExecuteNonQuery();
    }

    public class PdfConvertedImageModel
    {
        public int Id { get; set; }
        public string DatabaseName { get; set; } = "";
        public int TransactionId { get; set; }
        public string PdfFileName { get; set; } = "";
        public int PageNumber { get; set; }
        public string ImagePath { get; set; } = "";
        public DateTime CreatedDateTime { get; set; } = DateTime.Now;
    }
}