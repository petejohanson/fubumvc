﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FubuCore.Reflection;
using FubuCore.Util;
using FubuFastPack.Domain;
using FubuFastPack.Querying;

namespace FubuFastPack.JqGrid
{
    public class GridDefinition<T> : IGridDefinition where T : DomainEntity
    {
        private const int INITIAL_MAX_PAGE_COUNT = 1000;
        private readonly List<IGridColumn> _columns = new List<IGridColumn>();

        public GridDefinition()
        {
            _columns.Add(new DataColumn<T>());
            SortBy = SortRule<T>.Ascending(x => x.Id);
            MaxCount = INITIAL_MAX_PAGE_COUNT;
            CanSaveQuery = true;
        }

        public SortRule<T> SortBy { get; set; }

        public int MaxCount { get; set; }
        public bool AllowCreationOfNew { get; set; }

        public bool CanSaveQuery
        {
            get; set;
        }

        public IEnumerable<IGridColumn> Columns
        {
            get { return _columns; }
        }

        public IEnumerable<Accessor> SelectedAccessors
        {
            get { return _columns.SelectMany(x => x.SelectAccessors()).Distinct(); }
        }

        public TColumn AddColumn<TColumn>(TColumn column) where TColumn : IGridColumn
        {
            _columns.Add(column);
            return column;
        }

        public GridColumn<T> Show(Expression<Func<T, object>> expression)
        {
            return AddColumn(new GridColumn<T>(expression));
        }

        public LinkColumn<T> ShowViewLink(Expression<Func<T, object>> expression)
        {
            return AddColumn(new LinkColumn<T>(expression));
        }

        public OtherEntityLinkExpression<TOther> ShowViewLinkForOther<TOther>(Expression<Func<T, TOther>> entityProperty) where TOther : DomainEntity
        {
            return new OtherEntityLinkExpression<TOther>(this, entityProperty);
        }

        public class OtherEntityLinkExpression<TOther> where TOther : DomainEntity
        {
            private readonly GridDefinition<T> _grid;
            private readonly Expression<Func<T, TOther>> _entityProperty;

            public OtherEntityLinkExpression(GridDefinition<T> grid, Expression<Func<T, TOther>> entityProperty)
            {
                _grid = grid;
                _entityProperty = entityProperty;
            }

            public LinkColumn<T> DisplayTextFrom(Expression<Func<TOther, object>> property)
            {
                var topAccessor = ReflectionHelper.GetAccessor(_entityProperty);
                var displayAccessor = topAccessor.GetChildAccessor(property);
                var idAccessor = topAccessor.GetChildAccessor<TOther>(x => x.Id);

                return _grid.AddColumn(new LinkColumn<T>(displayAccessor, idAccessor, typeof(TOther)));
            } 
        }

        public Expression<Func<T, object>> PropertyExpressionFor(string propertyName)
        {
            return _columns.SelectMany(x => x.AllAccessors()).First(x => x.Name == propertyName).ToExpression<T>();
        }
    }
}