using System.Web.Mvc;
using Dissertation_Thesis_SitesTextCrawler.Data;

namespace Dissertation_Thesis_SitesTextCrawler.Controllers
{
    public abstract class BaseController : Controller
    {
        private WebAppContext _dbContext;
       
        protected WebAppContext WebApiContext => _dbContext ?? (_dbContext = new WebAppContext());

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _dbContext?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}