﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using ShopBot.CustomCards;
using ShopBot.Models;
using ShopBot.Repository;

namespace ShopBot.Dialogs
{
    [Serializable]
    public class ManageBasketDialog : IDialog<object>
    {
        private const string BasketContents = "Show products in basket";
        private const string ProductRemoval = "Remove single products";
        private const string EmptyBasket = "Clear basket";

        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            PromptForBasketOptionSelection(context);
        }

        private void PromptForBasketOptionSelection(IDialogContext context)
        {
            var options = new List<string>
            {
                BasketContents,
                ProductRemoval,
                EmptyBasket
            };
            PromptDialog.Choice(context, AfterOptionSelection, options, "Manage basket:");
        }

        private async Task AfterOptionSelection(IDialogContext context, IAwaitable<string> result)
        {
            var optionSelected = await result;
            await ExecuteAction(context, optionSelected);
        }

        private async Task ExecuteAction(IDialogContext context, string optionSelected)
        {
            switch (optionSelected)
            {
                case BasketContents:
                    await ShowBasketContentsCard(context);
                    break;
                case ProductRemoval:
                    await ShowProductRemovalCard(context);
                    context.Wait(RemoveProductMessageRecievedAsync);
                    break;
                case EmptyBasket:
                    //TODO
                    break;
            }
        }

        private async Task ShowBasketContentsCard(IDialogContext context)
        {
            context.ConversationData.TryGetValue(BotStateRepository.ProductsInBasket, out IList<Product> products);

            Attachment attachment = new Attachment
            {
                ContentType = AdaptiveCard.ContentType,
                Content = CardFactory.GetProductsBasketCard(products)
            };

            var reply = context.MakeMessage();
            reply.Attachments.Add(attachment);

            await context.PostAsync(reply, CancellationToken.None);
            context.Done("Basket contents viewed");
        }
        
        private async Task ShowProductRemovalCard(IDialogContext context)
        {
            context.ConversationData.TryGetValue(BotStateRepository.ProductsInBasket, out IList<Product> products);
            
            Attachment attachment = new Attachment
            {
                ContentType = AdaptiveCard.ContentType,
                Content = CardFactory.DeleteProductsFromBasketCard(products)
            };

            var reply = context.MakeMessage();
            reply.Attachments.Add(attachment);

            await context.PostAsync(reply, CancellationToken.None);
        }

        private async Task RemoveProductMessageRecievedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var message = await result;
            var removed = ProductRemover.Of(message.Value).Remove(context);
            await context.PostAsync(removed.Any() ? 
                "Items removed from basket: \n\n* " + string.Join(" \n\n* ", removed):
                "No products removed");
            context.Done("Items deleted");
        }
    }
}