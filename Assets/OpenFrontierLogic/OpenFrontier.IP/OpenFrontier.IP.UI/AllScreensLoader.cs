using System;
using System.Collections;
using System.Collections.Generic;
using OpenFrontier.IP.UI.Screens;
using UnityEngine;

namespace OpenFrontier.IP.UI
{
	public class AllScreensLoader : MonoBehaviour
	{
		public ScreenNavigator ScreenNavigator;

		private void Start()
		{
			foreach (KeyValuePair<Type, ScreenBase> item in ScreenNavigator.ScreenNameMapping)
			{
				if (ScreenNavigator.GetLoadedScreen(item.Key) == null)
				{
					ScreenNavigationRequestSimple request = new ScreenNavigationRequestSimple
					{
						LoadOnly = true,
						IgnoreIfExistingRequest = true,
						ScreenType = item.Key
					};
					StartCoroutine(LoadAndNavigateToScreenCoroutine(request));
				}
			}
		}

		private IEnumerator LoadAndNavigateToScreenCoroutine(ScreenNavigationRequestSimple request)
		{
			ScreenNavigator.NavigateToScreen(request);
			yield return 0;
			request.ScreenResult.gameObject.SetActive(value: false);
		}
	}
}
