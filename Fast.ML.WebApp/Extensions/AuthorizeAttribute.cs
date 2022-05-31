// using System;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.AspNetCore.Mvc.Filters;
//
// namespace Fast.ML.WebApp.Extensions;
//
// [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
// public class AuthorizeAttribute : Microsoft.AspNetCore.Authorization.AuthorizeAttribute, IAuthorizationFilter
// {
//     public void OnAuthorization(AuthorizationFilterContext context)
//     {
//         var user = context.HttpContext.User;
//
//         if (user.Identity is {IsAuthenticated: false})
//         {
//             context.Result = new UnauthorizedResult();
//         }
//     }
// }