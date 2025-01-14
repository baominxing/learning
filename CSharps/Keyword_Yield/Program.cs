﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keyword_Yield
{
    /// <summary>
    /// yield使用场景：在一些特定的数据源里二次条件过滤时，不用再定义一个中间集合去存储，只需yield return 变量值,前台遍历调用遍历输出即可。
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {

            #region Sample1
            Sample1.Demonstration();
            #endregion
           
            Console.ReadKey();
        }

        
    }

    public class Sample1
    {
        internal static void Demonstration()
        {
            var result = Power(2, 8, string.Empty);

            Console.WriteLine(result.Count());

            foreach (int i in result)
            {
                Console.Write("{0} ", i);
            }
        }

        public static IEnumerable<int> Power(int number, int exponent, string s)
        {
            int result = 1;

            for (int i = 0; i < exponent; i++)
            {
                result = result * number;

                if (result > 100)
                {
                    yield return result;
                }
            }

            yield return 3;
            yield return 4;
            yield return 5;
        }
    }
}
