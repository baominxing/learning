﻿using FileCenter.Common;
using System;
using System.Net;
using System.Net.Http;
using System.Web.Http.Filters;

namespace FileCenter.Filters
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
    public class WebApiExceptionFilter : ExceptionFilterAttribute
    {
        //重写基类的异常处理方法
        public override void OnException(HttpActionExecutedContext actionExecutedContext)
        {
            //1.异常日志记录（正式项目里面一般是用log4net记录异常日志）
            var exceptionString = $"{actionExecutedContext.Exception.GetType().ToString()}:{actionExecutedContext.Exception.Message}——堆栈信息:{actionExecutedContext.Exception.StackTrace}";
            var actionName = actionExecutedContext.ActionContext.ActionDescriptor.ActionName;
            var controllerName = actionExecutedContext.ActionContext.ActionDescriptor.ControllerDescriptor.ControllerName;

            LogHelper.Info($"[Execution of {controllerName} - {actionName} Occurs Error : {Environment.NewLine} " + $"{exceptionString}");

            //2.返回调用方具体的异常信息
            if (actionExecutedContext.Exception is NotImplementedException)
            {
                actionExecutedContext.Response = new HttpResponseMessage(HttpStatusCode.NotImplemented);
            }
            else if (actionExecutedContext.Exception is TimeoutException)
            {
                actionExecutedContext.Response = new HttpResponseMessage(HttpStatusCode.RequestTimeout);
            }
            else if (actionExecutedContext.Exception is NullReferenceException)
            {
                actionExecutedContext.Response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
            else
            {
                actionExecutedContext.Response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }

            actionExecutedContext.Response.Content = new StringContent(exceptionString);

            base.OnException(actionExecutedContext);
        }
    }
}