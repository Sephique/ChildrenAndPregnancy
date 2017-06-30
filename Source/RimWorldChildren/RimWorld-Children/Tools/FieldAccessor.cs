using System;
using System.Linq.Expressions;

namespace RimWorldChildren {

	internal static class FieldAccessor
	{
		public static Func<T,R> GetFieldAccessor<T,R>(string fieldName) 
		{ 
			ParameterExpression param = 
				Expression.Parameter (typeof(T),"arg");  

			MemberExpression member = 
				Expression.Field(param, fieldName);   

			LambdaExpression lambda = 
				Expression.Lambda(typeof(Func<T,R>), member, param);   

			Func<T,R> compiled = (Func<T,R>)lambda.Compile(); 
			return compiled; 
		}
	}
}