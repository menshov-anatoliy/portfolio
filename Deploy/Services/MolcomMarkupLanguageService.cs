using Molcom.Domain.Deploy.Mml.Documents;
using Molcom.Domain.Deploy.Mml.Documents.Interfaces;
using Molcom.Domain.Deploy.Mml.Factories;
using Molcom.Domain.Deploy.Mml.Markups;
using Molcom.Domain.Deploy.Mml.Markups.Open;
using Molcom.Domain.Shared.Interfaces;

namespace Molcom.Domain.Deploy.SqlObjects.Services;

/// <summary>
/// Сервис сборки документа DSL
/// </summary>
public class MolcomMarkupLanguageService(IMarkupFactory markupFactory) : IMolcomMarkupLanguageService
{
	public const string StartTagNotFoundMessage = "Ошибка парсинга шаблона процедуры. Для закрывающего тега \"{0}\" метаразметки не найден открывающий.";
	public const string EndTagNotFoundMessage = "Ошибка парсинга шаблона процедуры. Не найден закрывающий блок метаразметки для открывающего блока с именем \"{0}\".";

	public const string ElementWithNameCanNotBeEmbedded = "Ошибка парсинга шаблона процедуры. Ошибка валидации блока метаразметки с именем \"{0}\".";
	public const string ElementWithIndexCanNotBeEmbedded = "Ошибка парсинга шаблона процедуры. Ошибка валидации блока метаразметки с индексом \"{0}\".";

	public const string ElementWithNameCanNotContainAnyBlocks = "Ошибка парсинга шаблона процедуры. Блок метаразметки с именем \"{0}\" не может содержать вложенных блоков.";
	public const string ElementWithIndexCanNotContainAnyElements = "Ошибка парсинга шаблона процедуры. Блок метаразметки с индексом \"{0}\" не может содержать вложенных блоков.";

	public const string ElementWithNameCanNotContainTextElements = "Ошибка парсинга шаблона процедуры. Блок метаразметки с именем \"{0}\" не может содержать текст.";
	public const string ElementWithIndexCanNotContainTextElements = "Ошибка парсинга шаблона процедуры. Блок метаразметки с индексом \"{0}\" не может содержать текст.";

	private class DocumentElementStack(RootDocumentElement rootDocumentElements)
		: Stack<BaseDocumentElement>(new[] { rootDocumentElements });

	public RootDocumentElement CreateDocument(IValidationHandler validationHandler, string content)
	{
		var lines = content.Split(BaseDocumentElement.ElementSeparator);

		var rootMarkup = markupFactory.Create(null);

		var rootDocumentElement = (RootDocumentElement)rootMarkup.CreateDocument();

		var documentObjectsStack = new DocumentElementStack(rootDocumentElement);

		for (var index = 0; index < lines.Length; index++)
		{
			var currentLine = lines[index];

			var currentDocumentElement = documentObjectsStack.Peek();

			var markup = markupFactory.Create(currentLine);

			if (markup is OpenMarkupTag openMarkupTag)
			{
				var newDocumentElement = openMarkupTag.CreateDocument();

				ToAppendable(validationHandler, currentDocumentElement, index).Append(newDocumentElement);

				documentObjectsStack.Push(newDocumentElement);

				continue;
			}

			if (markup is CloseMarkupTag closeMarkupTag)
			{
				if (documentObjectsStack.Count == 0)
					throw validationHandler.UserError("Ошибка парсинга шаблона процедуры. Для закрывающего тега метаразметки не найден родительский контейнер.");

				currentDocumentElement = documentObjectsStack.Pop();

				if (currentDocumentElement is not NamedDocumentElement currentNamedDocumentElement)
					throw validationHandler.UserError(string.Format(StartTagNotFoundMessage, closeMarkupTag.Name));

				if (currentNamedDocumentElement.Name.Equals(closeMarkupTag.Name) == false)
					throw validationHandler.UserError(string.Format(EndTagNotFoundMessage, currentNamedDocumentElement.Name));

				currentNamedDocumentElement.Close(closeMarkupTag.Text!);

				continue;
			}

			if (markup is TextMarkup textMarkup)
			{
				var newTextLineDocumentElement = (TextLineDocumentElement)textMarkup.CreateDocument();

				ToTextAppendable(validationHandler, currentDocumentElement, index).Append(newTextLineDocumentElement);

				continue;
			}
		}

		return rootDocumentElement;
	}

	private static IAppendableDocumentElement ToAppendable(IValidationHandler validationHandler,
		BaseDocumentElement element,
		int index)
	{
		if (element is not IAppendableDocumentElement currentAppendableDocumentElement)
		{
			if (element is NamedDocumentElement currentBlockDocumentElement)
				throw validationHandler.UserError(string.Format(ElementWithNameCanNotContainAnyBlocks, currentBlockDocumentElement.Name));

			throw validationHandler.UserError(string.Format(ElementWithIndexCanNotContainAnyElements, index + 1));
		}

		return currentAppendableDocumentElement;
	}

	private static IAppendableTextLinesDocumentElement ToTextAppendable(IValidationHandler validationHandler,
		BaseDocumentElement element,
		int index)
	{
		if (element is not IAppendableTextLinesDocumentElement currentAppendableTextLinesDocumentElement)
		{
			if (element is NamedDocumentElement currentBlockDocumentElement)
				throw validationHandler.UserError(string.Format(ElementWithNameCanNotContainTextElements, currentBlockDocumentElement.Name));

			throw validationHandler.UserError(string.Format(ElementWithIndexCanNotContainTextElements, index + 1));
		}

		return currentAppendableTextLinesDocumentElement;
	}
}

public interface IMolcomMarkupLanguageService
{
	RootDocumentElement CreateDocument(IValidationHandler validationHandler, string content);
}
