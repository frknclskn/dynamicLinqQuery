using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace BusinessLayer.ExtensionMethods
{
    /// <summary>
    /// Gelen parametreye göre verileri sorguya hazırlar.
    /// </summary>
    public class LinqExpression
    {
        /// <summary>
        /// Gönderilen ifadelerin içerik sorgusunu hazırlar ve döner.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public Expression<Func<T, bool>> getLambda<T>(LinqExpressionAllParameters parameters = null) where T : class, new()
        {
            ///sorgu başlığı oluşturuldu örn:p=>p.UserId==2 deki gibi.
            ParameterExpression argParam = Expression.Parameter(typeof(T), "p");

            // İlk parametre
            Expression exp = null;
            // Birden fazla sorgu geldiyse her geleni ekleyecek alan.
            Expression expRight = null;
            BinaryExpression andExp = null;

            foreach (LinqExpressionParameter parameter in parameters.LinqExpressionParameters)
            {
                if (!string.IsNullOrEmpty(parameter.Value))
                {
                    Expression prop = Expression.Property(argParam, parameter.ColumnName);
                    ConstantExpression val = _getType(parameter, prop);
                    try
                    {
                        //İlki oluşturalım.
                        if (exp == null)
                        {
                            //Tek kez koşul sağlanırsa çıkışı kendisi olsun diye, zaten ikinci kez girer ve aşağıdaki else'e girerken güncellenecek.
                            exp = _getExpresion(parameter, prop, val);
                            andExp = (BinaryExpression)exp;
                        }
                        else
                        {
                            //Şuanki elde bulunan değerin sorgusunu oluşturur.(p=>p.UserId==1)...
                            expRight = _getExpresion(parameter, prop, val);

                            //Sorgu tipi and mi or mu ?(p=>p.UserId==1 && p.UserName=="test")...
                            andExp = _getQueryType(parameter, andExp, expRight);
                        }
                    }
                    catch (Exception)
                    {
                        //tip dönüşümünde hata çıktı
                        return null;
                    }
                }
            }
            Expression<Func<T, bool>> lambda = null;
            if (andExp != null)
                lambda = Expression.Lambda<Func<T, bool>>(andExp, argParam);

            return lambda;
        }
        /// <summary>
        /// Sorguya girin her eleman için and mi or mu kontrolünü sağlar.
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="andExp"></param>
        /// <param name="expRight"></param>
        /// <returns></returns>
        private BinaryExpression _getQueryType(LinqExpressionParameter parameter, BinaryExpression andExp, Expression expRight)
        {
            if (parameter.QueryType == QueryTypes.AndAlso)
            {
                andExp = Expression.AndAlso(andExp, expRight);
            }
            else if (parameter.QueryType == QueryTypes.OrElse)
            {
                andExp = Expression.OrElse(andExp, expRight);
            }
            return andExp;
        }

        /// <summary>
        /// expression'I doldurup yollar.
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="prop"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        private Expression _getExpresion(LinqExpressionParameter parameter, Expression prop, ConstantExpression val)
        {
            try
            {
                Expression exp = null;
                //Tek kez koşul sağlanırsa çıkışı kendisi olsun diye, zaten ikinci kez girer ve aşağıdaki else'e girerken güncellenecek.
                if (parameter.ExpressionType == ExpressionTypes.Equal)
                {
                    exp = Expression.Equal(prop, val);
                }
                else if (parameter.ExpressionType == ExpressionTypes.GreaterThanOrEqual)//>=
                {
                    exp = Expression.GreaterThanOrEqual(prop, val);
                }
                else if (parameter.ExpressionType == ExpressionTypes.LessThanOrEqual)//<=
                {
                    exp = Expression.LessThanOrEqual(prop, val);
                }
                else if (parameter.ExpressionType == ExpressionTypes.NotEqual)//!=
                {
                    exp = Expression.NotEqual(prop, val);
                }
                return exp;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// İlgili parametrenin tip dönüşümünü yapar veriyi doldurup yollar.
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="prop"></param>
        /// <returns></returns>
        private ConstantExpression _getType(LinqExpressionParameter parameter, Expression prop)
        {
            try
            {
                ConstantExpression val = null;
                Type type = prop.Type;

                if (type == typeof(Int32))
                    val = Expression.Constant(int.Parse(parameter.Value));
                else if (type == typeof(float))
                    val = Expression.Constant(float.Parse(parameter.Value));
                else if (type == typeof(double))
                    val = Expression.Constant(double.Parse(parameter.Value));
                else if (type == typeof(bool))
                    val = Expression.Constant(bool.Parse(parameter.Value));
                else if (type == typeof(string))
                    val = Expression.Constant(parameter.Value.ToString());
                else if (type == typeof(DateTime))
                    val = Expression.Constant(DateTime.Parse(parameter.Value));
                else if (type == typeof(TimeSpan))
                    val = Expression.Constant(TimeSpan.Parse(parameter.Value));
                return val;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}

/// <summary>
/// Sorguya gönderilecek tüm liste
/// </summary>
public class LinqExpressionAllParameters
{
    public List<LinqExpressionParameter> LinqExpressionParameters { get; set; } = new List<LinqExpressionParameter>();
}

/// <summary>
/// Tabloda bulunan ve sorguda kullanılacak her bir sütun ve istenen değerini içinde barındıran sınıftır.
/// </summary>
public class LinqExpressionParameter
{
    /// <summary>
    /// Tabloda bulunan sütunun adıdır.
    /// </summary>
    public string ColumnName { get; set; }
    public string Value { get; set; }
    public ExpressionTypes ExpressionType { get; set; } = ExpressionTypes.Equal;
    public QueryTypes QueryType { get; set; } = QueryTypes.AndAlso;
}
/// <summary>
/// gelen verinin nasıl eşleşeceğini belirler ==, !=, <=, >= gibi.
/// </summary>
public enum ExpressionTypes
{
    /// <summary>
    /// == expression
    /// </summary>
    Equal,
    /// <summary>
    /// >= expression
    /// </summary>
    GreaterThanOrEqual,
    /// <summary>
    /// <= expression
    /// </summary>
    LessThanOrEqual,
    /// <summary>
    /// != expression
    /// </summary>
    NotEqual
}
/// <summary>
/// Birden fazla expression için hangi tür sorgu oluşacağını belirler and mi or mu
/// </summary>
public enum QueryTypes
{
    /// <summary>
    /// && expression
    /// </summary>
    AndAlso,
    /// <summary>
    /// || expression
    /// </summary>
    OrElse
}