﻿using System;
using System.Text.Json.Nodes;
using Json.JsonE.Expressions;
using Json.More;

namespace Json.JsonE.Operators;

internal class IfThenElseOperator : IOperator
{
	public const string Name = "$if";

	public static readonly JsonNode DeleteMarker = "delete_marker"!;

	public void Validate(JsonNode? template)
	{
		var obj = template!.AsObject();

		obj.VerifyNoUndefinedProperties(Name, "then", "else");

		var parameter = obj[Name];
		if (parameter is not JsonValue value || !value.TryGetValue(out string? source))
			throw new TemplateException("$eval must be given a string expression");

		int index = 0;
		if (!ExpressionParser.TryParse(source.AsSpan(), ref index, out _))
			throw new TemplateException("Expression is not valid");
	}

	public JsonNode? Evaluate(JsonNode? template, EvaluationContext context)
	{
		var obj = template!.AsObject();
		var source = obj[Name]!.GetValue<string>();

		int index = 0;
		if (!ExpressionParser.TryParse(source.AsSpan(), ref index, out var expression))
			throw new TemplateException("Expression is not valid"); // shouldn't happen because we checked it earl

		var cond = expression!.Evaluate(context);
		var thenPresent = obj.TryGetValue("then", out var thenValue, out _);
		var elsePresent = obj.TryGetValue("else", out var elseValue, out _);

		thenValue = JsonE.Evaluate(thenValue, context);
		elseValue = JsonE.Evaluate(elseValue, context);

		return cond.IsTruthy()
			? thenPresent ? thenValue : DeleteMarker
			: elsePresent ? elseValue : DeleteMarker;
	}
}