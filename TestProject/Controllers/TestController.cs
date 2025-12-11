using Microsoft.AspNetCore.Mvc;

namespace TestProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly ILogger<TestController> _logger;
        private readonly string _directoryPath;

        public TestController(ILogger<TestController> logger, IConfiguration configuration)
        {
            _logger = logger;
            var homeDirectory = configuration["FileServer:HomeDirectory"] ?? "wwwroot";
            _directoryPath = Path.Combine(Directory.GetCurrentDirectory(), homeDirectory);
        }

        [HttpGet("browse")]
        public IActionResult Browse(string path = "")
        {
            try
            {
                string fullPath = Path.Combine(_directoryPath, path ?? "");
                fullPath = Path.GetFullPath(fullPath);

                if (!fullPath.StartsWith(_directoryPath))
                {
                    return BadRequest("Invalid path");
                }

                if (!Directory.Exists(fullPath))
                {
                    return NotFound("Directory not found");
                }

                var directoryInfo = new DirectoryInfo(fullPath);
                var result = new
                {
                    path = path,
                    directories = directoryInfo.GetDirectories()
                        .Select(d => new { name = d.Name, type = "directory" })
                        .OrderBy(d => d.name)
                        .ToList(),
                    files = directoryInfo.GetFiles()
                        .Select(f => new { name = f.Name, type = "file", size = f.Length })
                        .OrderBy(f => f.name)
                        .ToList()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error browsing directory");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("search")]
        public IActionResult Search(string query = "", string path = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest("Search query cannot be empty");
                }

                string fullPath = CombinePaths(_directoryPath, path ?? "");
                fullPath = Path.GetFullPath(fullPath);

                if (!fullPath.StartsWith(_directoryPath))
                {
                    return BadRequest("Invalid path");
                }

                if (!Directory.Exists(fullPath))
                {
                    return NotFound("Directory not found");
                }

                var searchResults = SearchRecursive(fullPath, query.ToLower());

                var result = new
                {
                    query = query,
                    path = path,
                    totalResults = searchResults.Count,
                    directories = searchResults
                        .Where(r => r.type == "directory")
                        .ToList(),
                    files = searchResults
                        .Where(r => r.type == "file")
                        .ToList()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching files");
                return StatusCode(500, "Internal server error");
            }
        }

        private List<dynamic> SearchRecursive(string basePath, string query)
        {
            var results = new List<dynamic>();

            try
            {
                var directoryInfo = new DirectoryInfo(basePath);

                foreach (var dir in directoryInfo.GetDirectories())
                {
                    if (dir.Name.ToLower().Contains(query))
                    {
                        var relativePath = GetRelativePath(dir.FullName);
                        results.Add(new
                        {
                            name = dir.Name,
                            type = "directory",
                            fullPath = relativePath
                        });
                    }

                    results.AddRange(SearchRecursive(dir.FullName, query));
                }

                foreach (var file in directoryInfo.GetFiles())
                {
                    if (file.Name.ToLower().Contains(query))
                    {
                        var relativePath = GetRelativePath(file.FullName);
                        results.Add(new
                        {
                            name = file.Name,
                            type = "file",
                            fullPath = relativePath,
                            size = file.Length
                        });
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Access denied while searching");
            }

            return results;
        }

        private string GetRelativePath(string fullPath)
        {
            if (fullPath.StartsWith(_directoryPath))
            {
                var relative = fullPath.Substring(_directoryPath.Length).TrimStart(Path.DirectorySeparatorChar);
                return relative.Replace(Path.DirectorySeparatorChar, '/');
            }

            return fullPath;
        }

        private string CombinePaths(string basePath, string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return basePath;
            }

            relativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(basePath, relativePath);
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(string path = "")
        {
            try
            {
                string uploadPath = CombinePaths(_directoryPath, path ?? "");
                uploadPath = Path.GetFullPath(uploadPath);

                if (!uploadPath.StartsWith(_directoryPath))
                {
                    return BadRequest("Invalid path");
                }

                if (!Directory.Exists(uploadPath))
                {
                    return NotFound("Directory not found");
                }

                var files = Request.Form.Files;
                if (files.Count == 0)
                {
                    return BadRequest("No files provided");
                }

                int uploadedCount = 0;
                var failedUploads = new List<string>();

                foreach (var file in files)
                {
                    try
                    {
                        if (file.Length > 0)
                        {
                            string filename = file.FileName;

                            filename = filename.Replace('/', Path.DirectorySeparatorChar);

                            string filePath = Path.Combine(uploadPath, filename);
                            filePath = Path.GetFullPath(filePath);

                            if (!filePath.StartsWith(uploadPath))
                            {
                                failedUploads.Add($"{Path.GetFileName(filename)} (path traversal detected)");
                                continue;
                            }

                            string? fileDirectory = Path.GetDirectoryName(filePath);
                            if (!string.IsNullOrEmpty(fileDirectory) && !Directory.Exists(fileDirectory))
                            {
                                Directory.CreateDirectory(fileDirectory);
                            }

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            uploadedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to upload file: {file.FileName}");
                        failedUploads.Add(Path.GetFileName(file.FileName));
                    }
                }

                string message = $"{uploadedCount} item(s) uploaded successfully";
                if (failedUploads.Count > 0)
                {
                    message += $". Failed: {string.Join(", ", failedUploads)}";
                }

                return Ok(new
                {
                    message = message,
                    uploadedCount = uploadedCount,
                    failedCount = failedUploads.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading files");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("download")]
        public IActionResult Download(string path = "")
        {
            try
            {
                string filePath = CombinePaths(_directoryPath, path ?? "");
                filePath = Path.GetFullPath(filePath);

                if (!filePath.StartsWith(_directoryPath))
                {
                    return BadRequest("Invalid path");
                }

                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound("File not found");
                }

                var stream = System.IO.File.OpenRead(filePath);
                var fileName = Path.GetFileName(filePath);
                return File(stream, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("delete")]
        public IActionResult Delete(string path = "", bool isDirectory = false)
        {
            try
            {
                string fullPath = CombinePaths(_directoryPath, path ?? "");
                fullPath = Path.GetFullPath(fullPath);

                if (!fullPath.StartsWith(_directoryPath))
                {
                    return BadRequest("Invalid path");
                }

                if (isDirectory)
                {
                    if (!Directory.Exists(fullPath))
                    {
                        return NotFound("Directory not found");
                    }
                    Directory.Delete(fullPath, recursive: true);
                }
                else
                {
                    if (!System.IO.File.Exists(fullPath))
                    {
                        return NotFound("File not found");
                    }
                    System.IO.File.Delete(fullPath);
                }

                return Ok(new { message = "Item deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting item");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}