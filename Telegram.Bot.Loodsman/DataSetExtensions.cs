using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;
using DataProvider;

namespace Telegram.Bot.Loodsman
{
	/// <summary>
	/// Расширающие методы для интерфейса IDataSet
	/// </summary>
	static class DataSetExtensions
	{
		/// <summary>
		/// Возвращает значение поля IDataSet как Int32
		/// </summary>
		/// <param name="ds">Набор данных</param>
		/// <param name="name">Имя поля</param>
		/// <returns>Значение поля</returns>
		public static int ValueAsInt(this IDataSet ds, string name)
		{
			if (Convert.IsDBNull(ds.get_FieldValue(name)))
				return 0;
			return (int)ds.get_FieldValue(name);
		}

		/// <summary>
		/// Возвращает значение поля IDataSet как double
		/// </summary>
		/// <param name="ds">Набор данных</param>
		/// <param name="name">Имя поля</param>
		/// <returns>Значение поля</returns>
		public static double ValueAsDouble(this IDataSet ds, string name)
		{
			if (Convert.IsDBNull(ds.get_FieldValue(name)))
				return 0;
			return (double)ds.get_FieldValue(name);
		}

		/// <summary>
		/// Возвращает значение поля IDataSet как string
		/// </summary>
		/// <param name="ds">Набор данных</param>
		/// <param name="name">Имя поля</param>
		/// <returns>Значение поля</returns>
		public static string ValueAsString(this IDataSet ds, string name)
		{
			if (Convert.IsDBNull(ds.get_FieldValue(name)))
				return String.Empty;
			return (string)ds.get_FieldValue(name);
		}

		/// <summary>
		/// Возвращает значение поля IDataSet как DateTime
		/// </summary>
		/// <param name="ds">Набор данных</param>
		/// <param name="name">Имя поля</param>
		/// <returns>Значение поля</returns>
		public static DateTime ValueAsDateTime(this IDataSet ds, string name)
		{
			if (Convert.IsDBNull(ds.get_FieldValue(name)))
				return DateTime.MinValue;
			return (DateTime)ds.get_FieldValue(name);
		}
	}
}
