using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DatenbankManagementSystem.Pages {
    public class ImpressumModel : PageModel {
        private readonly ILogger<ImpressumModel> _logger;

        public ImpressumModel(ILogger<ImpressumModel> logger) {
            _logger = logger;
        }

        public void OnGet() {
        }
    }
}