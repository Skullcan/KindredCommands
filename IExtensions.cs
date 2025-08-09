using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KindredCommands;
internal static class IExtensions
{
	static readonly System.Random _random = new();
	public static bool IsIndexWithinRange<T>(this IList<T> list, int index)
	{
		return index >= 0 && index < list.Count;
	}
	public static T DrawRandom<T>(this IList<T> list)
	{
		int index = _random.Next(list.Count);

		if (list.IsIndexWithinRange(index))
			return list[index];

		return default;
	}	
}
