﻿using System;
using System.Collections;
using Foundation;
using UIKit;

namespace Xamarin.Forms.Platform.iOS
{
	// TODO hartez 2018/06/01 14:17:00 Implement Dispose override ?	
	// TODO hartez 2018/06/01 14:21:24 Add a method for updating the layout	
	internal class CollectionViewController : UICollectionViewController
	{
		readonly IEnumerable _itemsSource;
		readonly ItemsView _itemsView;
		readonly ItemsViewLayout _layout;
		bool _initialConstraintsSet;

		public CollectionViewController(ItemsView itemsView, ItemsViewLayout layout) : base(layout)
		{
			_itemsView = itemsView;
			_itemsSource = _itemsView.ItemsSource;
			_layout = layout;

			_layout.GetPrototype = GetProtoType;
			_layout.UniformSize = false; // todo hartez Link this to ItemsView.ItemSizingStrategy hint
		}

		public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
		{
			var cell = collectionView.DequeueReusableCell(DetermineCellReusedId(), indexPath) as UICollectionViewCell;

			switch (cell)
			{
				case DefaultCell defaultCell:
					UpdateDefaultCell(defaultCell, indexPath);
					break;
				case TemplatedCell templatedCell:
					UpdateTemplatedCell(templatedCell, indexPath);
					break;
			}

			return cell;
		}

		public override nint GetItemsCount(UICollectionView collectionView, nint section)
		{
			// TODO hartez 2018/06/07 17:06:18 Obviously this needs to handle things which are not ILists	
			return (_itemsSource as IList).Count;
		}

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();
			AutomaticallyAdjustsScrollViewInsets = false;
			RegisterCells();
			CollectionView.WeakDelegate = _layout;
		}

		public override void ViewWillLayoutSubviews()
		{
			base.ViewWillLayoutSubviews();

			// We can't set this constraint up on ViewDidLoad, because Forms does other stuff that resizes the view
			// and we end up with massive layout errors. And View[Will/Did]Appear do not fire for this controller
			// reliably. So until one of those options is cleared up, we set this flag so that the initial constraints
			// are set up the first time this method is called.
			if (!_initialConstraintsSet)
			{
				_layout.ConstrainTo(CollectionView.Bounds.Size);
				_initialConstraintsSet = true;
			}
		}

		protected virtual void UpdateDefaultCell(DefaultCell cell, NSIndexPath indexPath)
		{
			if (_itemsSource is IList list)
			{
				cell.Label.Text = list[indexPath.Row].ToString();
			}

			if (cell is IConstrainedCell constrainedCell)
			{
				_layout.PrepareCellForLayout(constrainedCell);
			}
		}

		protected virtual void UpdateTemplatedCell(TemplatedCell cell, NSIndexPath indexPath)
		{
			ApplyTemplateAndDataContext(cell, indexPath);

			if (cell is IConstrainedCell constrainedCell)
			{
				_layout.PrepareCellForLayout(constrainedCell);
			}
		}

		void ApplyTemplateAndDataContext(TemplatedCell cell, NSIndexPath indexPath)
		{
			// We need to create a renderer, which means we need a template
			var templateElement = _itemsView.ItemTemplate.CreateContent() as View;
			IVisualElementRenderer renderer = CreateRenderer(templateElement);

			if (_itemsSource is IList list && renderer != null)
			{
				BindableObject.SetInheritedBindingContext(renderer.Element, list[indexPath.Row]);
				cell.SetRenderer(renderer);
			}
		}

		IVisualElementRenderer CreateRenderer(View view)
		{
			if (view == null)
			{
				throw new ArgumentNullException(nameof(view));
			}

			var renderer = Platform.CreateRenderer(view);
			Platform.SetRenderer(view, renderer);

			return renderer;
		}

		string DetermineCellReusedId()
		{
			if (_itemsView.ItemTemplate != null)
			{
				return _layout.ScrollDirection == UICollectionViewScrollDirection.Horizontal
					? TemplatedHorizontalListCell.ReuseId
					: TemplatedVerticalListCell.ReuseId;
			}

			return _layout.ScrollDirection == UICollectionViewScrollDirection.Horizontal
				? DefaultHorizontalListCell.ReuseId
				: DefaultVerticalListCell.ReuseId;
		}

		UICollectionViewCell GetProtoType()
		{
			// TODO hartez assuming this works, we'll need to evaluate using this nsindexpath (what about groups?)
			// TODO hartez Also, what about situations where there is no data which matches the path?
			var indexPath = NSIndexPath.Create(0, 0);
			return GetCell(CollectionView, indexPath);
		}

		void RegisterCells()
		{
			CollectionView.RegisterClassForCell(typeof(DefaultHorizontalListCell), DefaultHorizontalListCell.ReuseId);
			CollectionView.RegisterClassForCell(typeof(DefaultVerticalListCell), DefaultVerticalListCell.ReuseId);
			CollectionView.RegisterClassForCell(typeof(TemplatedHorizontalListCell),
				TemplatedHorizontalListCell.ReuseId);
			CollectionView.RegisterClassForCell(typeof(TemplatedVerticalListCell), TemplatedVerticalListCell.ReuseId);
		}
	}
}