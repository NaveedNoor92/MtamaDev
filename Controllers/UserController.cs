﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mtama.Controllers;
using Mtama.Data;
using Mtama.Models;
using Mtama.Services;

namespace Mtama.Controllers
{
    public class UserController : Controller
    {
        private RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(
          ApplicationDbContext context,
          UserManager<ApplicationUser> userManager,
          RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [Authorize(Roles = "Super Admin, Admin, Aggregator")]
        public async Task<IActionResult> ViewUsers(string UserStatus = "")
        {
            var model = await _context.Users.ToListAsync();
            foreach (var item in model)
            {
                var UserRole = await _userManager.GetRolesAsync(item);
                item.UserRole = UserRole[0];
            }
            if (UserStatus != "")
            {
                ViewBag.StatusMessage = UserStatus;
            }
            return View(model);
        }

        public async Task<IActionResult> AddUser()
        {
            return View("/Views/Account/Register.cshtml");
        }

        //[HttpPost]
        //[AllowAnonymous]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> AddUser(RegisterViewModel model, string returnUrl = null)
        //{
        //    ViewData["ReturnUrl"] = returnUrl;
        //    try
        //    {

        //        if (ModelState.IsValid)
        //        {
        //            var user = new ApplicationUser
        //            {
        //                FirstName = model.FirstName,
        //                LastName = model.LastName,
        //                Farmer_Id_Form_No = model.NationalId,
        //                //  Dob = model.Dob,       
        //                Gender = model.Gender,
        //                //  KraPin = model.KraPin,
        //                PhoneNumber = model.MobileNumber,
        //                Email = model.Email,
        //                UserName = model.MobileNumber
        //            };

        //            var result = await _userManager.CreateAsync(user, model.Password);
        //            if (result.Succeeded)
        //            {
        //                _logger.LogInformation("User created a new account with password.");

        //                //var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        //                //var callbackUrl = Url.EmailConfirmationLink(user.Id, code, Request.Scheme);
        //                //await _emailSender.SendEmailConfirmationAsync(model.Email, callbackUrl);

        //                //await _signInManager.SignInAsync(user, isPersistent: false);
        //                _logger.LogInformation("User created a new account with password.");
        //                //                    return RedirectToLocal(returnUrl);
        //                return RedirectToAction("ViewUsers", "User", new { area = "" });
        //            }
        //            //AddErrors(result);
        //            //  ViewData["Error"] = result.Errors.FirstOrDefault().Description;
        //            ViewData["Error"] = " The Phonenumber you entered is already taken. Please enter a different one.";
        //            return View(model);
        //        }
        //        // If we got this far, something failed, redisplay form
        //        return View(model);
        //        //   // return RedirectToAction("HOmeIndex");
        //    }
        //    catch (Exception ex)
        //    {
        //        ViewData["Error"] = ex.Message;
        //        return View(model);
        //    }


        //}

        [Authorize(Roles = "Super Admin")]
        [HttpGet]
        public async Task<IActionResult> ManageUserRoles(string id, string StatusMessage )
        {

            ViewBag.userId = id;
            ViewBag.StatusMessage = StatusMessage;
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                ViewBag.ErrorMessage = $"User with Id = {id} cannot be found";
                ViewBag.StatusMessage = $"Error: User with Id =  {id} cannot be found";
                return RedirectToAction("ViewUsers") ;
            }

            var UserRole = await _userManager.GetRolesAsync(user);

            var model = new List<UserRoleViewModel>();
            foreach (var role in _roleManager.Roles)
            {
                var userRoleViewModel = new UserRoleViewModel
                {
                    RoleId = role.Id,
                    RoleName = role.Name
                };
                if (role.Name == UserRole[0])
                {
                    userRoleViewModel.IsSelected = true;
                }
                else
                {
                    userRoleViewModel.IsSelected = false;
                }



                //if (await _userManager.IsInRoleAsync(user, role.Name))
                //{
                //    userRoleViewModel.IsSelected = true;
                //}
                //else {
                //    userRoleViewModel.IsSelected = false;
                //}
                model.Add(userRoleViewModel);
            }

            return View(model);
        }

        [Authorize(Roles = "Super Admin")]
        [HttpPost]
        public async Task<IActionResult> ManageUserRoles(List<UserRoleViewModel> model, string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"User with Id = {id} cannot be found";
                ViewBag.StatusMessage = $"Error: User with Id = { id} cannot be found";
                return RedirectToAction("ViewUser");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var result = await _userManager.RemoveFromRolesAsync(user, roles);

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Cannot remove user existing roles");
                ViewBag.StatusMessage = "Error: Cannot remove user existing roles";
                return View(model);
            }

            result = await _userManager.AddToRolesAsync(user, model.Where(x => x.IsSelected).Select(y => y.RoleName));

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Cannot add user existing roles");
                ViewBag.StatusMessage = "Error: Cannot add user existing roles";
                return View(model);
            }

            var StatusMessage = "User role successfully updated.";
            return RedirectToAction("ManageUserRoles", new { id = id , StatusMessage = StatusMessage });
        }
    }
}
