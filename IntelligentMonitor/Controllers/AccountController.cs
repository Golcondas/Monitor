﻿using IntelligentMonitor.Models.Users;
using IntelligentMonitor.Providers.Users;
using IntelligentMonitor.Utility;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IntelligentMonitor.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserProvider _provider;

        public AccountController(UserProvider provider)
        {
            _provider = provider;
        }

        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login([Bind("UserName", "Password")]LoginViewModel vm)
        {
            if (ModelState.IsValid)
            {
                var user = _provider.GetUser(vm.UserName, vm.Password);
                if (user == null)
                {
                    return Json(new { code = 1, msg = "账号或密码错误！" });
                }
                else
                {
                    var claimIdentity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
                    claimIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
                    claimIdentity.AddClaim(new Claim(ClaimTypes.Name, user.NickName));
                    claimIdentity.AddClaim(new Claim(ClaimTypes.Role, user.RoleName));

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimIdentity));
                }
            }

            return Json(new { code = 0, msg = "登录成功！" });
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        public IActionResult Profile()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Profile(Users user)
        {
            int code = await _provider.UpdateUser(user);

            return code > 0 ? Json(new { code = 0, msg = "修改成功！" }) : Json(new { code = 1, msg = "请稍后再试！" });
        }

        public IActionResult Password()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Password([Bind("OldPassword", "Password", "Repassword")]PasswordViewModel vm)
        {
            var code = 0;
            var msg = "";

            var userIdClaim = HttpContext.User.FindFirst(u => u.Type == ClaimTypes.NameIdentifier);
            var id = Convert.ToInt32(userIdClaim.Value);

            var pass = await _provider.VerifyPassword(id, vm.OldPassword);
            if (!pass)
            {
                code = 1;
                msg = "旧密码错误！";
                return Json(new { code, msg });
            }

            var user = _provider.GetUser(id);
            user.Password = MD5Util.TextToMD5(vm.Repassword);
            var result = await _provider.UpdateUser(user);

            return result > 0 ? Json(new { code, msg = "密码修改成功！" }) : Json(new { code = 1, msg = "请稍后再试！" });
        }
    }
}