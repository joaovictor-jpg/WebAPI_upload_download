using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Upload.Data;
using Upload.Models.StatusResponse;
using Upload.Respository;

namespace Upload.Controllers
{
    public class FileController : ApiController
    {

        [HttpPost]
        public async Task<IHttpActionResult> Upload()
        {

            var ctx = HttpContext.Current;
            var root = ctx.Server.MapPath("~/App_Data");
            var provider = new MultipartFormDataStreamProvider(root);

            try
            {
                await Request.Content.ReadAsMultipartAsync(provider);

                foreach (var file in provider.FileData)
                {

                    var name = file.Headers
                        .ContentDisposition
                        .FileName;

                    // remove double aspa duplo from string
                    name = name.Trim('"');

                    var fileExtension = Path.GetExtension(name).ToLower();
                    if (fileExtension == ".exe" || fileExtension == ".bet")
                        return BadRequest("extension not accepted!");

                    var localFileName = file.LocalFileName;

                    var filePath = Path.Combine(root, "files", name);

                    SaveFileRepository.SaveFileBinarySQLServerADO(localFileName, name);

                }
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest($"Erro: {ex.Message}");
            }
            catch (Exception ex)
            {
                return BadRequest($"Erro: {ex.Message}");
            }

            return new TextResult("File upload", Request);
        }

        [HttpGet]
        public HttpResponseMessage GetFile(int id)
        {
            var result = new HttpResponseMessage(HttpStatusCode.OK);

            var fileName = "";
            var fileBytes = new byte[0];

            // Query
            var q = "SELECT name, size, FileBin FROM Files WHERE id=@id;";

            // Read file bytes from database
            using (var conn = new SqlConnection(Base.connStr))
            using (var cmd = new SqlCommand(q, conn))
            {
                cmd.Parameters.Add(
                        "@id",
                        SqlDbType.Int)
                        .Value = id;

                conn.Open();

                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    fileName = reader.GetString(0);
                    var size = reader.GetInt32(1);
                    fileBytes = new byte[size];
                    reader.GetBytes(2, 0, fileBytes, 0, size);
                }

                conn.Close();
            }

            if(fileBytes.Length == 0)
            {
                result.StatusCode = HttpStatusCode.NotFound;
            }
            else
            {
                // Add bytes to a memory stream
                var fileMemStream =
                    new MemoryStream(fileBytes);

                result.Content = new StreamContent(fileMemStream);

                var headers = result.Content.Headers;

                headers.ContentDisposition =
                    new ContentDispositionHeaderValue("attachment");
                headers.ContentDisposition.FileName = fileName;

                headers.ContentType =
                    new MediaTypeHeaderValue("application/octet-stream");

                headers.ContentLength = fileMemStream.Length;
            }

            return result;
        }
    }
}
