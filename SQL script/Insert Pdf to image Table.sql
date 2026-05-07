
GO

IF EXISTS(SELECT * FROM SYSOBJECTS where [name]  = 'PdfConvertedImages' AND xtype = 'U')
BEGIN
CREATE TABLE dbo.PdfConvertedImages
(
    Id            INT IDENTITY PRIMARY KEY,
    DatabaseName  VARCHAR(10) NOT NULL,
     TransactionId    INT         NOT NULL,
    TransactionTypeId    INT         NOT NULL,
    PdfFileName   NVARCHAR(255) NOT NULL,
    PageNumber    INT         NOT NULL,
    ImagePath     NVARCHAR(500) NOT NULL,
    CreatedOn     DATETIME2    NOT NULL DEFAULT SYSDATETIME()
);

CREATE INDEX IX_PdfConvertedImages_UQ
ON dbo.PdfConvertedImages(DatabaseName, TransactionId, PdfFileName, PageNumber);

END
GO

