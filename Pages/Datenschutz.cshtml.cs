using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DatenbankManagementSystem.Pages {
    public class DatenschutzModel : PageModel {
        private readonly ILogger<ImpressumModel> _logger;

        public DatenschutzModel(ILogger<ImpressumModel> logger) {
            _logger = logger;
        }

        public void OnGet() {
        }
    }
}