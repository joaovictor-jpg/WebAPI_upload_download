using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Web;
using System.IO;
using Upload.Data;

namespace Upload.Respository
{
    public class SaveFileRepository
    {
        private static void SaveFilePathSQLServerADO(string localFile, string filePath)
        {
            // Move file to folder
            File.Move(localFile, filePath);

            //Insert in DB
            var q = "INSERT INTO FileLocation(FilePath) VALUES (@FilePath);";

            using (var conn = new SqlConnection(Base.connStr))
            using (var cmd = new SqlCommand(q, conn))
            {
                cmd.Parameters.Add("@FilePath", SqlDbType.VarChar, 200).Value = filePath;
                conn.Open();
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }

        internal static void SaveFileBinarySQLServerADO(
            string localFile, string fileName)
        {
            // Get file binary
            byte[] fileBytes;
            using (var fs = new FileStream(
                localFile, FileMode.Open, FileAccess.Read))
            {
                fileBytes = new byte[fs.Length];
                if(fileBytes.Length == new byte[10100000].Length)
                {
                    throw new InvalidOperationException("the maximum is 9MB");
                }
                fs.Read(
                    fileBytes, 0, Convert.ToInt32(fs.Length));
            }

            var query =
                "Insert into Files(FileBin, Name, Size) " +
                "values (@FileBin, @Name, @Size);";

            using (var conn = new SqlConnection(Base.connStr))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.Add(
                    "@FileBin",
                    SqlDbType.VarBinary)
                    .Value = fileBytes;

                cmd.Parameters.Add(
                    "@Name",
                    SqlDbType.VarChar, 50)
                    .Value = fileName;

                cmd.Parameters.Add(
                    "@Size",
                    SqlDbType.Int)
                    .Value = fileBytes.Length;

                conn.Open();
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }
    }
}