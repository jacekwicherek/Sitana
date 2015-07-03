﻿// /// This file is a part of the EBATIANOS.ESSENTIALS class library.
// /// (c)2013-2014 EBATIANO'S a.k.a. Sebastian Sejud. All rights reserved.
// ///
// /// THIS SOURCE FILE IS THE PROPERTY OF EBATIANO'S A.K.A. SEBASTIAN SEJUD 
// /// AND IS NOT TO BE RE-DISTRIBUTED BY ANY MEANS WHATSOEVER WITHOUT 
// /// THE EXPRESSED WRITTEN CONSENT OF EBATIANO'S A.K.A. SEBASTIAN SEJUD.
// ///
// /// THIS SOURCE CODE CAN ONLY BE USED UNDER THE TERMS AND CONDITIONS OUTLINED
// /// IN THE EBATIANOS.ESSENTIALS LICENSE AGREEMENT. 
// /// EBATIANO'S A.K.A. SEBASTIAN SEJUD GRANTS TO YOU (ONE SOFTWARE DEVELOPER) 
// /// THE LIMITED RIGHT TO USE THIS SOFTWARE ON A SINGLE COMPUTER.
// ///
// /// CONTACT INFORMATION:
// /// contact@ebatianos.com
// /// www.ebatianos.com/essentials-library
// /// 
// ///---------------------------------------------------------------------------
//
using System;
using Microsoft.Xna.Framework;
using Android.Widget;
using Android.Views.InputMethods;
using Android.Content;
using Android.Util;
using Android.Graphics.Drawables;
using Android.Graphics.Drawables.Shapes;
using Android.Text.Method;
using Android.Views;
using Android.Text;
using Sitana.Framework.Ui.Core;
using Sitana.Framework.Input.Interfaces;
using Sitana.Framework.Misc;

namespace Sitana.Framework.Input
{
	public partial class NativeInput
    {
		class BackableSkipper: IBackable
		{
			public bool OnBack()
			{
				UiTask.BeginInvoke(()=>ExtendedKeyboardManager.Instance.Remove(this));
				return true;
			}
		}

        static EditTextEx _textField = null;
        bool _internalTextChange = false;
        ITextEdit _controller;

		static NativeInput CurrentFocus = null;

        static RelativeLayout.LayoutParams _layoutParams;

        public bool Visible
        {
            get
            {
                return true;
            }
        }

        private void InitTextField()
        {
            _layoutParams = new RelativeLayout.LayoutParams(10,10);
            _layoutParams.SetMargins(0,0, 0, 0);

			_textField = CreateEdit(_layoutParams);

            var drawable = new ShapeDrawable(new RectShape());
            drawable.Paint.StrokeWidth = 0;
            drawable.Paint.SetStyle(Android.Graphics.Paint.Style.Fill);
            drawable.Paint.Color = new Android.Graphics.Color(255, 255, 255);

            _textField.SetBackgroundDrawable(drawable);
			_textField.SetTextColor(Android.Graphics.Color.Black);

            _textField.SetIncludeFontPadding(false);
            _textField.Visibility = Android.Views.ViewStates.Invisible;

			_textField.SetHighlightColor(new Android.Graphics.Color(128, 128, 128));
			_textField.SetHintTextColor(new Android.Graphics.Color(128, 128, 128));

			_textField.OnBackPressed += UnfocusByBack;
        }

		EditTextEx CreateEdit(ViewGroup.LayoutParams lp)
		{
			var edit = new EditTextEx(AppMain.Activity);
			edit.LayoutParameters = lp;

			AppMain.Current.RootView.AddView(edit);
			return edit;
		}

		public NativeInput(Rectangle position, TextInputType textInputType, string text, int textSize, Align textAlign, ITextEdit controller)
        {
            if (CurrentFocus != null)
            {
                CurrentFocus.Unfocus();
            }

            _controller = controller;
            CurrentFocus = this;

			if (_textField != null)
			{
				EditTextEx field = _textField;
				field.OnBackPressed -= UnfocusByBack;
				_textField = null;

				DelayedActionInvoker.Instance.AddAction(10, (s) =>
				{
					AppMain.Current.RootView.RemoveView(field);
				});
			}

            {
                InitTextField();
            }

			DisplayMetrics metrics = AppMain.Activity.Resources.DisplayMetrics;

			int padding = 0;

            if (textInputType == TextInputType.MultilineText)
            {
                padding = 0;
            }

			_textField.SetTextSize(Android.Util.ComplexUnitType.Px, (float)(UiUnit.FontUnit * textSize));
            _textField.SetHeight(position.Height);
            _textField.SetPadding(0, padding, _textField.PaddingRight, padding);
            _textField.InputType = TypeFromContext(textInputType);

			if (textInputType.HasFlag(TextInputType.Uppercase))
			{
				_textField.SetFilters(new IInputFilter[] { new InputFilterLengthFilter(controller.MaxLength), new InputFilterAllCaps() });
			}
			else
			{
				_textField.SetFilters(new IInputFilter[] { new InputFilterLengthFilter(controller.MaxLength) });
			}

            switch (textAlign & Align.Horz)
            {
            case Align.Left:
                _textField.Gravity = GravityFlags.CenterVertical | GravityFlags.Left;
                break;

            case Align.Center:
                _textField.Gravity = GravityFlags.CenterVertical | GravityFlags.CenterHorizontal;
                break;

            case Align.Right:
                _textField.Gravity = GravityFlags.CenterVertical | GravityFlags.Right;
                break;
            }


			if ((textInputType&TextInputType.TypeFilter) == TextInputType.MultilineText)
            {
                _textField.SetMaxLines(controller.MaxLines);
                _textField.EditorAction -= HandleEditorAction;
                _textField.Gravity = GravityFlags.Left | GravityFlags.Top;
                _textField.SetSingleLine(false);

				_textField.ImeOptions = ImeAction.ImeNull | (ImeAction)ImeFlags.NoExtractUi;
            } 
            else
            {
                _textField.SetMaxLines(1);
                _textField.EditorAction += HandleEditorAction;
                _textField.SetSingleLine(true);

				_textField.ImeOptions = (controller.WaitsForReturn ? ImeAction.Next : ImeAction.Done) | (ImeAction)ImeFlags.NoExtractUi;
            }

			if (textInputType.HasFlag(TextInputType.NoSuggestions))
			{
				_textField.ImeOptions |= (ImeAction)InputTypes.TextFlagNoSuggestions;
			}

			_textField.SetAllCaps( (textInputType&TextInputType.TypeFilter) == TextInputType.Uppercase );

			_textField.TransformationMethod = textInputType == TextInputType.Password ? new PasswordTransformationMethod(): null;

            _layoutParams.SetMargins(position.X, position.Y + 4, 0, 0);
            _layoutParams.Width = position.Width;
            _layoutParams.Height = position.Height - 4;

            _textField.Text = text;
            _textField.SetSelection(_textField.Text.Length, _textField.Text.Length);
            _textField.TextChanged += HandleEditingChanged;
            _textField.FocusChange += HandleFocusChange;

            _textField.RequestLayout();

            _textField.Visibility = Android.Views.ViewStates.Visible;
			_textField.SetCursorVisible(true);
            _textField.RequestFocus();
			_textField.RequestFocusFromTouch();

			ShowKeyboard(_textField);
        }

        bool HandleEditorAction(Android.Views.InputMethods.ImeAction actionCode)
        {
            if ( CurrentFocus != null && CurrentFocus._controller != null )
            {
                CurrentFocus._controller.Return();
                return true;
            }

            return false;
        }

		void UnfocusByBack()
		{
			ExtendedKeyboardManager.Instance.Add(new BackableSkipper());
			Unfocus();
		}

        void HandleFocusChange(object sender, Android.Views.View.FocusChangeEventArgs e)
        {
            if (!e.HasFocus)
            {
                Unfocus();
                _controller.LostFocus();
            }
        }

        void HandleEditingChanged(object sender, EventArgs e)
        {
            if (_internalTextChange)
                return;

            String newText = _textField.Text;

            string text = _controller.TextChanged(newText);

            if (text != newText)
            {
                SetText(text);
            }
        }

        public void SetText(string text)
        {
            _internalTextChange = true;
            _textField.Text = text;
            _textField.SetSelection(text.Length, text.Length);
            _internalTextChange = false;
        }

        public void Unfocus()
        {
            _textField.Visibility = Android.Views.ViewStates.Invisible;

            _textField.TextChanged -= HandleEditingChanged;
            _textField.FocusChange -= HandleFocusChange;
            _textField.EditorAction -= HandleEditorAction;

            UiTask.BeginInvoke(() =>
            {
                if (CurrentFocus == this)
                {
                    HideKeyboard(_textField);
                }
            });
        }

		void HideKeyboard(View view) 
		{
			try
			{
				InputMethodManager imm = (InputMethodManager)AppMain.Activity.GetSystemService(Context.InputMethodService);
				imm.HideSoftInputFromWindow(view.WindowToken, 0);
			}
			catch{}
		}

		void ShowKeyboard(View view)
		{
			InputMethodManager imm = (InputMethodManager)AppMain.Activity.GetSystemService(Context.InputMethodService);
			imm.ShowSoftInput(view, ShowFlags.Forced);
		}

		InputTypes TypeFromContext(TextInputType context)
        {
			InputTypes value = Android.Text.InputTypes.TextFlagNoSuggestions |  Android.Text.InputTypes.ClassText;

			switch (context & TextInputType.TypeFilter)
            {
			case TextInputType.Email:
				value = InputTypes.TextVariationEmailAddress | Android.Text.InputTypes.ClassText;
				break;

			case TextInputType.FirstLetterUppercase:
				value = InputTypes.TextVariationPersonName | Android.Text.InputTypes.ClassText;
				break;

			case TextInputType.Digits:
				value = InputTypes.ClassNumber;
				break;

			case TextInputType.Numeric:
				value = InputTypes.NumberFlagDecimal | Android.Text.InputTypes.ClassNumber;
				break;

			case TextInputType.NormalText:
				value = InputTypes.TextVariationNormal | Android.Text.InputTypes.ClassText;
				break;

			case TextInputType.Uppercase:
				value = InputTypes.TextFlagCapCharacters | InputTypes.ClassText;
				break;

			case TextInputType.PasswordClass:
				value = InputTypes.TextVariationPassword | Android.Text.InputTypes.ClassText;
				break;

			case TextInputType.MultilineText:
				value = InputTypes.TextFlagMultiLine | Android.Text.InputTypes.TextFlagImeMultiLine;
				break;
            }

			if (context.HasFlag(TextInputType.NoSuggestions))
			{
				value |= InputTypes.TextFlagNoSuggestions | InputTypes.TextVariationVisiblePassword;
			}

			return value;
        }
    }
}


