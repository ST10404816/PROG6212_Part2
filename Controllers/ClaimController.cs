// Lisha Naidoo
// ST10404816
// Group 1

// References
// https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/
// https://dotnethow.net/a-step-by-step-guide-to-configuring-entity-framework-in-your-net-web-api-project/

using Microsoft.AspNetCore.Mvc;           
using Microsoft.EntityFrameworkCore;      
using POE.Data;                           
using POE.Models;                         

namespace POE.Controllers
{    
    public class ClaimController : Controller
    {
        private readonly AppDbContext _context;  // Dependency Injection of AppDbContext.

        //------------------------------------------------------------------------------------------------------------------------//
        // Constructor to inject the AppDbContext dependency.
        public ClaimController(AppDbContext context)
        {
            _context = context;
        }

        //------------------------------------------------------------------------------------------------------------------------//
        // Updates the status of a claim by ID.
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            // Find the claim with the given ID.
            var claim = await _context.Claims.FindAsync(id);
            if (claim == null) return NotFound(); // Return 404 if not found.

            // Update the status of the claim.
            claim.status = status;
            _context.Update(claim);
            await _context.SaveChangesAsync();  // Save changes to the database.

            // Set a success message to display on the next page.
            TempData["SuccessMessage"] = $"Claim has been {status.ToLower()}!";
            return RedirectToAction("ViewClaims");  // Redirect to ViewClaims page.
        }

        //------------------------------------------------------------------------------------------------------------------------//
        // Retrieves and displays a list of all claims.
        public async Task<IActionResult> ViewClaims()
        {
            // Get all claims from the database.
            var claims = await _context.Claims.ToListAsync();
            return View(claims);  // Pass claims to the view.
        }

        //------------------------------------------------------------------------------------------------------------------------//
        // Creates a new claim with an optional file upload.
        [HttpPost]
        public async Task<IActionResult> Create(Claim claim, IFormFile? uploadedFile)
        {
            // Check if a file was uploaded and its size is greater than 0.
            if (uploadedFile != null && uploadedFile.Length > 0)
            {
                // Define the allowed file extensions.
                var allowedExtensions = new[] { ".pdf", ".docx", ".xlsx" };
                var fileExtension = Path.GetExtension(uploadedFile.FileName).ToLower();

                // Check if the uploaded file has a valid extension.
                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("UploadedFilePath",
                        "Only PDF, DOCX, and XLSX files are allowed.");
                }
                else
                {
                    // Generate a unique file path to save the uploaded file.
                    var filePath = Path.Combine("wwwroot/uploads", Guid.NewGuid() + fileExtension);

                    try
                    {
                        // Save the uploaded file to the generated path.
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await uploadedFile.CopyToAsync(stream);
                        }
                        // Save the file path to the claim.
                        claim.UploadedFilePath = "/uploads/" + Path.GetFileName(filePath);
                    }
                    catch (Exception ex)
                    {
                        // Log the error and display an error message to the user.
                        ModelState.AddModelError("",
                            $"An error occurred while uploading the file: {ex.Message}");
                    }
                }
            }

            // Check if the model state is valid before saving the claim.
            if (ModelState.IsValid)
            {
                _context.Add(claim);               // Add the claim to the database.
                await _context.SaveChangesAsync();  // Save changes to the database.
                return RedirectToAction("Success"); // Redirect to success page.
            }

            // Log validation errors if any.
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                Console.WriteLine(string.Join(", ", errors));  // Output errors to console.
                return View(claim);  // Return the view with validation errors.
            }

            // Return the view if there were issues during claim creation.
            return View(claim);
        }

        //------------------------------------------------------------------------------------------------------------------------//
        // Displays the claim creation form.
        public IActionResult Create()
        {
            return View();  // Return the claim creation view.
        }

        //------------------------------------------------------------------------------------------------------------------------//
        // Downloads a file based on the provided file path.
        public IActionResult DownloadFile(string filePath)
        {
            // Generate the full path to the file on the server.
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", filePath.TrimStart('/'));

            // Check if the file exists.
            if (!System.IO.File.Exists(fullPath))
            {
                return NotFound("File not found.");  // Return 404 if file is missing.
            }

            // Read the file into a byte array and return it for download.
            byte[] fileBytes = System.IO.File.ReadAllBytes(fullPath);
            return File(fileBytes, "application/octet-stream", Path.GetFileName(fullPath));
        }

        //------------------------------------------------------------------------------------------------------------------------//
        // Displays the main page.
        public IActionResult Index()
        {
            return View();  
        }

        //------------------------------------------------------------------------------------------------------------------------//
        // Displays a form to edit claims.
        public async Task<IActionResult> Edit()
        {
            // Get all claims from the database.
            var claims = await _context.Claims.ToListAsync();

            // If no claims are found, display an error message and redirect.
            if (!claims.Any())
            {
                TempData["ErrorMessage"] = "No claims found.";
                return RedirectToAction("ViewClaims");
            }

            return View(claims);  // Return the view with the list of claims.
        }

        //------------------------------------------------------------------------------------------------------------------------//
        // Updates an existing claim with new information.
        [HttpPost]
        public async Task<IActionResult> Update(Claim claim)
        {
            // Ensure the model is valid before proceeding.
            if (!ModelState.IsValid) return View(claim);

            try
            {
                // Calculate the total based on hours worked and hourly rate.
                claim.Total = claim.HoursWorked * claim.HourlyRate;

                // Update the claim in the database.
                _context.Claims.Update(claim);
                await _context.SaveChangesAsync();  // Save changes to the database.

                TempData["SuccessMessage"] = "Claim updated successfully.";
                return RedirectToAction("Success");  // Redirect to success page.
            }
            catch (Exception ex)
            {
                // Handle exceptions and display error message to the user.
                ModelState.AddModelError("", $"Error: {ex.Message}");
                return View(claim);
            }
        }

        //------------------------------------------------------------------------------------------------------------------------//
        // Deletes a claim by ID.
        public async Task<IActionResult> Delete(int? id)
        {
            // Check if the ID is null.
            if (id == null) return NotFound();

            // Find the claim with the given ID.
            var claim = await _context.Claims.FindAsync(id);
            if (claim == null) return NotFound();  // Return 404 if not found.

            // Remove the claim from the database.
            _context.Claims.Remove(claim);
            await _context.SaveChangesAsync();  // Save changes to the database.

            return RedirectToAction(nameof(Index));  // Redirect to index page.
        }
    }
}
//------------------------------------------...ooo000 END OF FILE 000ooo...------------------------------------------------------//