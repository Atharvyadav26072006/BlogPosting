using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using SyncSyntax.Data;
using SyncSyntax.Models;
using SyncSyntax.Models.ViewModels;

namespace SyncSyntax.Controllers
{


    public class PostController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly string[] _allowedExtension = { ".jpg", ".jpeg", ".png" };

        public PostController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;

        }
        [HttpGet]

        public IActionResult Index(int? categoreyId)
        {
            var postQuery = _context.Posts.Include(p => p.Category).AsQueryable();
            if (categoreyId.HasValue)

            {
                postQuery = postQuery.Where(p => p.CategoryId == categoreyId);
            }
            var posts = postQuery.ToList();

            ViewBag.Categories = _context.Categories.ToList();
            return View(posts);
        }

        [HttpGet]

        public async Task<IActionResult> Detail(int id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var post = _context.Posts.Include(p => p.Category).Include(p => p.Comments).FirstOrDefault(p => p.Id == id);

            if (id == null)
            {
                return NotFound();
            }

            return View(post);

        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Create()

        {
            var postViewModel = new PostViewModel();
            postViewModel.Categories = _context.Categories.Select(c =>
                  new SelectListItem
                  {
                      Value = c.Id.ToString(),
                      Text = c.Name
                  }

            ).ToList();

            return View(postViewModel);
        }



        //[HttpPost]
        //[Authorize(Roles = "Admin")]
        //public async Task<IActionResult> Create(PostViewModel postViewModel)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        var inputFileExtension = Path.GetExtension(postViewModel.FeatureImage.FileName).ToLower();
        //        bool isAllowed = _allowedExtension.Contains(inputFileExtension);

        //        if (!isAllowed)
        //        {
        //            ModelState.AddModelError("", "Invalid Image Format.Allowed Format are .jpg,.jpeg,.png");
        //            return View(postViewModel);

        //        }

        //        postViewModel.Post.FeatureImagePath = await UploadFiletoFolder(postViewModel.FeatureImage);
        //        await _context.Posts.AddAsync(postViewModel.Post);
        //        await _context.SaveChangesAsync();
        //        return RedirectToAction("Index");

        //    }
        //    postViewModel.Categories = _context.Categories.Select(c =>
        //  new SelectListItem
        //  {
        //      Value = c.Id.ToString(),
        //      Text = c.Name
        //  }

        //        ).ToList();

        //    return View(postViewModel);
        //}


        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(PostViewModel postViewModel)
        {
            if (ModelState.IsValid)
            {
                // ✅ FIX: Null check
                if (postViewModel.FeatureImage != null)
                {
                    var inputFileExtension = Path.GetExtension(postViewModel.FeatureImage.FileName).ToLower();
                    bool isAllowed = _allowedExtension.Contains(inputFileExtension);

                    if (!isAllowed)
                    {
                        ModelState.AddModelError("", "Invalid Image Format. Allowed: .jpg, .jpeg, .png");
                        return View(postViewModel);
                    }

                    postViewModel.Post.FeatureImagePath = await UploadFiletoFolder(postViewModel.FeatureImage);
                }
                else
                {
                    // optional default image
                    postViewModel.Post.FeatureImagePath = "";
                }

                await _context.Posts.AddAsync(postViewModel.Post);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            // reload categories
            postViewModel.Categories = _context.Categories.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            }).ToList();

            return View(postViewModel);
        }












        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var postFormDb = await _context.Posts.FirstOrDefaultAsync(p => p.Id == id);

            if (postFormDb == null)
            {
                return NotFound();
            }

            EditViewModel editViewModel = new EditViewModel
            {
                Post = postFormDb,


                Categories = _context.Categories.Select(c =>
                  new SelectListItem
                  {
                      Value = c.Id.ToString(),
                      Text = c.Name
                  }

            ).ToList()

            };
            return View(editViewModel);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(EditViewModel editViewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(editViewModel);
            }
            var postFromDb = await _context.Posts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == editViewModel.Post.Id);

            if (postFromDb == null)
            {
                return NotFound();
            }
            if (editViewModel.FeatureImage != null)
            {
                var inputFileExtension = Path.GetExtension(editViewModel.FeatureImage.FileName).ToLower();
                bool isAllowed = _allowedExtension.Contains(inputFileExtension);

                if (!isAllowed)
                {
                    ModelState.AddModelError("", "Invalid Image Format.Allowed Format are .jpg,.jpeg,.png");
                    return View(editViewModel);

                }
                var existingFilePath = Path.Combine(_webHostEnvironment.WebRootPath, "Images", Path.GetFileName(postFromDb.FeatureImagePath));

                if (System.IO.File.Exists(existingFilePath))
                {
                    System.IO.File.Delete(existingFilePath);
                }

                editViewModel.Post.FeatureImagePath = await UploadFiletoFolder(editViewModel.FeatureImage);
            }
            else
            {
                editViewModel.Post.FeatureImagePath = postFromDb.FeatureImagePath;
            }
            _context.Posts.Update(editViewModel.Post);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var postFormDb = await _context.Posts.FirstOrDefaultAsync(p => p.Id == id);
            if (postFormDb == null)
            {
                return NotFound();
            }
            return View(postFormDb);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirm(int id)
        {
            var postFormDb = await _context.Posts.FirstOrDefaultAsync(p => p.Id == id);

            if (string.IsNullOrEmpty(postFormDb.FeatureImagePath))
            {
                var existingFilePath = Path.Combine(_webHostEnvironment.WebRootPath, "Images", Path.GetFileName(postFormDb.FeatureImagePath));

                if (System.IO.File.Exists(existingFilePath))
                {
                    System.IO.File.Delete(existingFilePath);
                }

            }
            _context.Posts.Remove(postFormDb);
            _context.SaveChangesAsync();
            return RedirectToAction("Index");

        }

        [Authorize]
        public JsonResult AddComment([FromBody] Comment comment)
        {
            comment.CommentDate = DateTime.Now;
            _context.Comments.Add(comment);
            _context.SaveChanges();

            return Json(new
            {
                username = comment.UserName,
                commentDate = comment.CommentDate.ToString("MMMM dd,yyyy"),
                content = comment.Content,

            });
        }


        private async Task<string> UploadFiletoFolder(IFormFile file)
        {

            var inputFileExtension = Path.GetExtension(file.FileName);
            var fileName = Guid.NewGuid().ToString() + inputFileExtension;
            var wwwRootPath = _webHostEnvironment.WebRootPath;
            var imagesFolderPath = Path.Combine(wwwRootPath, "images");

            if (!Directory.Exists(imagesFolderPath))
            {
                Directory.CreateDirectory(imagesFolderPath);
            }

            var filePath = Path.Combine(imagesFolderPath, fileName);
            try
            {
                await using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
            }
            catch (Exception ex)
            {
                return "Error Uploading images :" + ex.Message;

            }
            return "/images/" + fileName;
        }
    }
}