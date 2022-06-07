using System.Web.Mvc;
using Dissertation_Thesis_WebsiteScraper.Data;

namespace Dissertation_Thesis_WebsiteScraper.Controllers
{
    public abstract class BaseController : Controller
    {
        private WebAppContext _dbContext;
       
        protected WebAppContext WebApiContext => _dbContext ?? (_dbContext = new WebAppContext());

    }
}