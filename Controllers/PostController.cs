using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SyncSyntax.Data;
using SyncSyntax.Models;
using SyncSyntax.Models.ViewModels;

namespace SyncSyntax.Controllers
{
    public class PostController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        private readonly string[] _allowedExtensions =
        {
            ".jpg",
            ".jpeg",
            ".png"
        };

        public PostController(
            AppDbContext context,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // -------------------------------------------------------
        // POST LIST
        // -------------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> Index(int? categoryId)
        {
            var postQuery = _context.Posts
                .Include(p => p.Category)
                .AsQueryable();

            if (categoryId.HasValue)
            {
                postQuery = postQuery.Where(
                    p => p.CategoryId == categoryId.Value);
            }

            var posts = await postQuery
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            ViewBag.Categories = await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(posts);
        }

        // -------------------------------------------------------
        // POST DETAILS
        // -------------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var post = await _context.Posts
                .Include(p => p.Category)
                .Include(p => p.Comments)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
            {
                return NotFound();
            }

            return View(post);
        }

        // -------------------------------------------------------
        // CREATE POST - GET
        // -------------------------------------------------------

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            var postViewModel = new PostViewModel
            {
                Post = new Post(),
                Categories = await GetCategoriesAsync()
            };

            return View(postViewModel);
        }

        // -------------------------------------------------------
        // CREATE POST - POST
        // -------------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(
            PostViewModel postViewModel)
        {
            if (postViewModel.Post == null)
            {
                ModelState.AddModelError(
                    "",
                    "Post information is required.");

                postViewModel.Post = new Post();
            }

            var categoryExists = await _context.Categories
                .AnyAsync(c =>
                    c.Id == postViewModel.Post.CategoryId);

            if (!categoryExists)
            {
                ModelState.AddModelError(
                    "Post.CategoryId",
                    "Please select a valid category.");
            }

            if (postViewModel.FeatureImage != null)
            {
                var extension = Path
                    .GetExtension(postViewModel.FeatureImage.FileName)
                    .ToLowerInvariant();

                if (!_allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(
                        "FeatureImage",
                        "Only JPG, JPEG and PNG images are allowed.");
                }
            }

            if (!ModelState.IsValid)
            {
                postViewModel.Categories =
                    await GetCategoriesAsync();

                return View(postViewModel);
            }

            try
            {
                if (postViewModel.FeatureImage != null)
                {
                    postViewModel.Post.FeatureImagePath =
                        await UploadFileToFolder(
                            postViewModel.FeatureImage);
                }
                else
                {
                    postViewModel.Post.FeatureImagePath =
                        string.Empty;
                }

                await _context.Posts.AddAsync(
                    postViewModel.Post);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Post created successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine("CREATE POST DATABASE ERROR");
                Console.WriteLine(ex.InnerException?.Message);
                Console.WriteLine(ex);

                ModelState.AddModelError(
                    "",
                    ex.InnerException?.Message
                    ?? "Unable to save the post.");

                postViewModel.Categories =
                    await GetCategoriesAsync();

                return View(postViewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine("CREATE POST ERROR");
                Console.WriteLine(ex);

                ModelState.AddModelError(
                    "",
                    "An unexpected error occurred while creating the post.");

                postViewModel.Categories =
                    await GetCategoriesAsync();

                return View(postViewModel);
            }
        }

        // -------------------------------------------------------
        // EDIT POST - GET
        // -------------------------------------------------------

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var postFromDatabase = await _context.Posts
                .FirstOrDefaultAsync(p => p.Id == id);

            if (postFromDatabase == null)
            {
                return NotFound();
            }

            var editViewModel = new EditViewModel
            {
                Post = postFromDatabase,
                Categories = await GetCategoriesAsync()
            };

            return View(editViewModel);
        }

        // -------------------------------------------------------
        // EDIT POST - POST
        // -------------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(
            EditViewModel editViewModel)
        {
            if (editViewModel.Post == null)
            {
                return BadRequest("Post information is required.");
            }

            var existingPost = await _context.Posts
                .FirstOrDefaultAsync(
                    p => p.Id == editViewModel.Post.Id);

            if (existingPost == null)
            {
                return NotFound();
            }

            var categoryExists = await _context.Categories
                .AnyAsync(c =>
                    c.Id == editViewModel.Post.CategoryId);

            if (!categoryExists)
            {
                ModelState.AddModelError(
                    "Post.CategoryId",
                    "Please select a valid category.");
            }

            if (editViewModel.FeatureImage != null)
            {
                var extension = Path
                    .GetExtension(editViewModel.FeatureImage.FileName)
                    .ToLowerInvariant();

                if (!_allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(
                        "FeatureImage",
                        "Only JPG, JPEG and PNG images are allowed.");
                }
            }

            if (!ModelState.IsValid)
            {
                editViewModel.Categories =
                    await GetCategoriesAsync();

                return View(editViewModel);
            }

            try
            {
                existingPost.Title =
                    editViewModel.Post.Title;

                existingPost.Content =
                    editViewModel.Post.Content;

                existingPost.Author =
                    editViewModel.Post.Author;

                existingPost.CategoryId =
                    editViewModel.Post.CategoryId;

                if (editViewModel.FeatureImage != null)
                {
                    DeleteImageFile(
                        existingPost.FeatureImagePath);

                    existingPost.FeatureImagePath =
                        await UploadFileToFolder(
                            editViewModel.FeatureImage);
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Post updated successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine("EDIT POST DATABASE ERROR");
                Console.WriteLine(ex.InnerException?.Message);
                Console.WriteLine(ex);

                ModelState.AddModelError(
                    "",
                    ex.InnerException?.Message
                    ?? "Unable to update the post.");

                editViewModel.Categories =
                    await GetCategoriesAsync();

                return View(editViewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine("EDIT POST ERROR");
                Console.WriteLine(ex);

                ModelState.AddModelError(
                    "",
                    "An unexpected error occurred while updating the post.");

                editViewModel.Categories =
                    await GetCategoriesAsync();

                return View(editViewModel);
            }
        }

        // -------------------------------------------------------
        // DELETE POST - GET
        // -------------------------------------------------------

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var postFromDatabase = await _context.Posts
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (postFromDatabase == null)
            {
                return NotFound();
            }

            return View(postFromDatabase);
        }

        // -------------------------------------------------------
        // DELETE POST - POST
        // -------------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirm(int id)
        {
            var postFromDatabase = await _context.Posts
                .FirstOrDefaultAsync(p => p.Id == id);

            if (postFromDatabase == null)
            {
                return NotFound();
            }

            try
            {
                DeleteImageFile(
                    postFromDatabase.FeatureImagePath);

                _context.Posts.Remove(postFromDatabase);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Post deleted successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine("DELETE POST DATABASE ERROR");
                Console.WriteLine(ex.InnerException?.Message);
                Console.WriteLine(ex);

                TempData["ErrorMessage"] =
                    ex.InnerException?.Message
                    ?? "Unable to delete the post.";

                return RedirectToAction(nameof(Index));
            }
        }

        // -------------------------------------------------------
        // ADD COMMENT
        // -------------------------------------------------------

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddComment(
            [FromBody] Comment comment)
        {
            if (comment == null)
            {
                return BadRequest("Comment data is required.");
            }

            if (string.IsNullOrWhiteSpace(comment.Content))
            {
                return BadRequest(
                    "Comment content is required.");
            }

            var postExists = await _context.Posts
                .AnyAsync(p => p.Id == comment.PostId);

            if (!postExists)
            {
                return BadRequest("Invalid post.");
            }

            try
            {
                // PostgreSQL timestamp with time zone requires UTC
                comment.CommentDate = DateTime.UtcNow;

                if (string.IsNullOrWhiteSpace(comment.UserName))
                {
                    comment.UserName =
                        User.Identity?.Name ?? "User";
                }

                await _context.Comments.AddAsync(comment);

                await _context.SaveChangesAsync();

                return Json(new
                {
                    username = comment.UserName,

                    commentDate = comment.CommentDate
                        .ToString("MMMM dd, yyyy"),

                    content = comment.Content
                });
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine("COMMENT DATABASE ERROR");
                Console.WriteLine(ex.InnerException?.Message);
                Console.WriteLine(ex);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ex.InnerException?.Message
                    ?? "Unable to save the comment.");
            }
        }

        // -------------------------------------------------------
        // GET CATEGORY DROPDOWN
        // -------------------------------------------------------

        private async Task<List<SelectListItem>>
            GetCategoriesAsync()
        {
            return await _context.Categories
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToListAsync();
        }

        // -------------------------------------------------------
        // UPLOAD IMAGE
        // -------------------------------------------------------

        private async Task<string> UploadFileToFolder(
            IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new InvalidOperationException(
                    "The selected image is empty.");
            }

            var extension = Path
                .GetExtension(file.FileName)
                .ToLowerInvariant();

            if (!_allowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException(
                    "Only JPG, JPEG and PNG images are allowed.");
            }

            var fileName =
                $"{Guid.NewGuid()}{extension}";

            var imagesFolderPath = Path.Combine(
                _webHostEnvironment.WebRootPath,
                "images");

            Directory.CreateDirectory(imagesFolderPath);

            var filePath = Path.Combine(
                imagesFolderPath,
                fileName);

            await using var fileStream = new FileStream(
                filePath,
                FileMode.Create);

            await file.CopyToAsync(fileStream);

            return $"/images/{fileName}";
        }

        // -------------------------------------------------------
        // DELETE IMAGE
        // -------------------------------------------------------

        private void DeleteImageFile(string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                return;
            }

            var fileName = Path.GetFileName(imagePath);

            if (string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }

            var filePath = Path.Combine(
                _webHostEnvironment.WebRootPath,
                "images",
                fileName);

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }
    }
}