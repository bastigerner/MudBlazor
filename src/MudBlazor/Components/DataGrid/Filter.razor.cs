// Copyright (c) MudBlazor 2021
// MudBlazor licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace MudBlazor
{
    public partial class Filter<T>
    {
        [CascadingParameter] public MudDataGrid<T> DataGrid { get; set; }

        [Parameter] public Guid Id { get; set; }
        [Parameter] public string Field { get; set; }
        [Parameter] public Type FieldType { get; set; }
        [Parameter] public string Title { get; set; }
        [Parameter] public string Operator { get; set; }
        [Parameter] public object Value { get; set; }
        [Parameter] public Column<T> Column { get; set; }
        [Parameter] public EventCallback<string> FieldChanged { get; set; }
        [Parameter] public EventCallback<string> TitleChanged { get; set; }
        [Parameter] public EventCallback<string> OperatorChanged { get; set; }
        [Parameter] public EventCallback<object> ValueChanged { get; set; }

        private string _valueString => Value?.ToString();
        private double? _valueNumber => Value == null ? null : Convert.ToDouble(Value);
        private Enum _valueEnum => (Enum)Value;
        private Enum _valueEnumFlags => (Enum)Value;
        private bool? _valueBool => Value == null ? null : Convert.ToBoolean(Value);
        private DateTime? _valueDate => Value == null ? DateTime.Now : Convert.ToDateTime(Value);
        private TimeSpan? _valueTime => Value == null ? DateTime.Now.TimeOfDay : Convert.ToDateTime(Value).TimeOfDay;

        #region Computed Properties and Functions

        private Type dataType
        {
            get
            {
                if (Column != null)
                    return Column.dataType;

                if (FieldType != null)
                    return FieldType;

                if (Field == null)
                    return typeof(object);

                if (typeof(T) == typeof(IDictionary<string, object>) && FieldType == null)
                    throw new ArgumentNullException(nameof(FieldType));

                var t = typeof(T).GetProperty(Field).PropertyType;
                return Nullable.GetUnderlyingType(t) ?? t;
            }
        }

        private bool isNumber
        {
            get
            {
                return FilterOperator.IsNumber(dataType);
            }
        }
        private bool isEnum
        {
            get
            {
                return FilterOperator.IsEnum(dataType);
            }
        }

        private bool isEnumFlags
        {
            get
            {
                return FilterOperator.IsEnumFlags(dataType);
            }
        }

        #endregion

        protected override void OnInitialized()
        {
            if (DataGrid == null)
                DataGrid = Column?.DataGrid;
        }


        internal void TitleChangedAsync(string field)
        {
            Field = field;
        }

        internal void StringValueChanged(string value)
        {
            Value = value;
            ValueChanged.InvokeAsync(value);
            DataGrid.GroupItems();
        }

        internal void NumberValueChanged(double? value)
        {
            Value = value;
            ValueChanged.InvokeAsync(value);
            DataGrid.GroupItems();
        }

        internal void EnumValueChanged(Enum value)
        {
            Value = value;
            ValueChanged.InvokeAsync(value);
            DataGrid.GroupItems();
        }

        internal void EnumFlagsValueChanged(IEnumerable<Enum> values)
        {
            var count = values.Count();
            if (count == 0)
                Value = null;
            else
            {
                Value = values.First();
                for (int i = 1; i < count; i++)
                    Value = _valueEnumFlags.Or(values.ElementAt(i));
            }
            ValueChanged.InvokeAsync(Value);
            DataGrid.GroupItems();
        }

        internal void BoolValueChanged(bool? value)
        {
            Value = value;
            ValueChanged.InvokeAsync(value);
            DataGrid.GroupItems();
        }

        internal void DateValueChanged(DateTime? value)
        {
            Value = value;

            if (value != null)
            {
                var date = value.Value.Date;

                // get the time component and add it to the date.
                if (_valueTime != null)
                {
                    date.Add(_valueTime.Value);
                }

                ValueChanged.InvokeAsync(date);
            }
            else
                ValueChanged.InvokeAsync(value);

            DataGrid.GroupItems();
        }

        internal void TimeValueChanged(TimeSpan? value)
        {
            Value = value;

            if (_valueDate != null)
            {
                var date = _valueDate.Value.Date;

                
                // get the time component and add it to the date.
                if (_valueTime != null)
                {
                    date = date.Add(_valueTime.Value);
                }

                ValueChanged.InvokeAsync(date);
            }

            DataGrid.GroupItems();
        }

        internal void RemoveFilter()
        {
            DataGrid.RemoveFilter(Id);
        }
    }
}

public static class Extensions
{
    public static Enum Or(this Enum a, Enum b)
    {
        // consider adding argument validation here
        if (Enum.GetUnderlyingType(a.GetType()) != typeof(ulong))
            return (Enum)Enum.ToObject(a.GetType(), Convert.ToInt64(a) | Convert.ToInt64(b));
        else
            return (Enum)Enum.ToObject(a.GetType(), Convert.ToUInt64(a) | Convert.ToUInt64(b));
    }

    public static Enum And(this Enum a, Enum b)
    {
        // consider adding argument validation here
        if (Enum.GetUnderlyingType(a.GetType()) != typeof(ulong))
            return (Enum)Enum.ToObject(a.GetType(), Convert.ToInt64(a) & Convert.ToInt64(b));
        else
            return (Enum)Enum.ToObject(a.GetType(), Convert.ToUInt64(a) & Convert.ToUInt64(b));
    }
    public static Enum Not(this Enum a)
    {
        // consider adding argument validation here
        return (Enum)Enum.ToObject(a.GetType(), ~Convert.ToInt64(a));
    }
}
