-- Users table
CREATE TABLE IF NOT EXISTS "Users" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Username" VARCHAR(255) NOT NULL UNIQUE,
    "Email" VARCHAR(255) NOT NULL UNIQUE,
    CONSTRAINT "CK_User_Username_NotEmpty" CHECK (LENGTH(TRIM("Username")) > 0),
    CONSTRAINT "CK_User_Email_NotEmpty" CHECK (LENGTH(TRIM("Email")) > 0)
);

-- Documents table
CREATE TABLE IF NOT EXISTS "Documents" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Title" VARCHAR(500) NOT NULL,
    "FileName" VARCHAR(500) NOT NULL,
    "MimeType" VARCHAR(255) NOT NULL,
    "Size" BIGINT NOT NULL,
    "UploadedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UploadedById" UUID NOT NULL,
    CONSTRAINT "FK_Documents_Users_UploadedById" FOREIGN KEY ("UploadedById") 
        REFERENCES "Users"("Id") ON DELETE CASCADE,
    CONSTRAINT "CK_Document_Title_NotEmpty" CHECK (LENGTH(TRIM("Title")) > 0),
    CONSTRAINT "CK_Document_FileName_NotEmpty" CHECK (LENGTH(TRIM("FileName")) > 0),
    CONSTRAINT "CK_Document_Size_Positive" CHECK ("Size" > 0)
);

-- Tags table
CREATE TABLE IF NOT EXISTS "Tags" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Name" VARCHAR(100) NOT NULL UNIQUE,
    CONSTRAINT "CK_Tag_Name_NotEmpty" CHECK (LENGTH(TRIM("Name")) > 0)
);

-- DocumentTags junction table (many-to-many relationship)
CREATE TABLE IF NOT EXISTS "DocumentTags" (
    "DocumentId" UUID NOT NULL,
    "TagId" UUID NOT NULL,
    PRIMARY KEY ("DocumentId", "TagId"),
    CONSTRAINT "FK_DocumentTags_Documents_DocumentId" FOREIGN KEY ("DocumentId") 
        REFERENCES "Documents"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_DocumentTags_Tags_TagId" FOREIGN KEY ("TagId") 
        REFERENCES "Tags"("Id") ON DELETE CASCADE
);

-- Create indexes for better query performance
CREATE INDEX IF NOT EXISTS "IX_Documents_UploadedById" ON "Documents"("UploadedById");
CREATE INDEX IF NOT EXISTS "IX_Documents_UploadedAt" ON "Documents"("UploadedAt");
CREATE INDEX IF NOT EXISTS "IX_DocumentTags_TagId" ON "DocumentTags"("TagId");
CREATE INDEX IF NOT EXISTS "IX_DocumentTags_DocumentId" ON "DocumentTags"("DocumentId");

