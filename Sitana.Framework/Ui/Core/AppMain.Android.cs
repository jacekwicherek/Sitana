﻿// SITANA - Copyright (C) The Sitana Team.
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.
using Android.App;
using Android.Views;
using Microsoft.Xna.Framework;
using Sitana.Framework.Diagnostics;
using System;
using Android.Widget;
using Sitana.Framework.Misc;


namespace Sitana.Framework.Ui.Core
{
	public partial class AppMain
	{
		public RelativeLayout RootView { get; private set; }

        public AppMain()
		{
			Init();
			AddView();

			Graphics.PreferredBackBufferWidth = Activity.Window.DecorView.Width;
			Graphics.PreferredBackBufferHeight = Activity.Window.DecorView.Height;
		}

		protected AppMain(bool dummy)
		{
		}

		protected void AddView()
		{
			RootView = new RelativeLayout(Activity);

			View view = (View)Services.GetService(typeof(View));
			RootView.AddView(view);

			Activity.SetContentView(RootView);

			RootView.KeyPress += OnKeyPress;
			view.KeyPress += OnKeyPress;
		}

		void OnKeyPress(object sender, View.KeyEventArgs e)
		{
			if (e.Event.Action == KeyEventActions.Up && e.Event.KeyCode == Keycode.Back) 
			{
				if (ExtendedKeyboardManager.Instance.OnBackPressed())
				{
					e.Handled = true;
				}
			}

			if (e.Event.Action == KeyEventActions.Up && e.Event.KeyCode == Keycode.Menu) 
			{
				if (ExtendedKeyboardManager.Instance.OnMenuPressed())
				{
					e.Handled = true;
				}
			}
		}
	}
}
