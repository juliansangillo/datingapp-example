using System;
using System.Threading.Tasks;
using API.Entities.DB;
using API.Extensions;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace API.Helpers {
	public class LogUserActivity : IAsyncActionFilter {
		public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
			ActionExecutedContext resultContext = await next();

            if(!resultContext.HttpContext.User.Identity.IsAuthenticated)
                return;

            int userId = resultContext.HttpContext.User.GetUserId();
            IUnitOfWork unitOfWork = resultContext.HttpContext.RequestServices.GetService<IUnitOfWork>();
            AppUser user = await unitOfWork.UserRepository.GetUserByIdAsync(userId);
            
            user.LastActive = DateTime.UtcNow;

            await unitOfWork.Complete();
		}
	}
}