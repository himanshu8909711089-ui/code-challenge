using CodeChallenge.Api.Logic;
using CodeChallenge.Api.Models;
using CodeChallenge.Api.Repositories;
using FluentAssertions;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace CodeChallenge.Tests;

public class MessageLogicTests
{
    private readonly Mock<IMessageRepository> _repository = new();

    [Fact]
    public async Task CreateMessageAsync_Success_ReturnsCreated()
    {
        var organizationId = Guid.NewGuid();
        var request = new CreateMessageRequest
        {
            Title = "Hello World",
            Content = new string('a', 20)
        };

        var createdMessage = new Message
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Title = request.Title,
            Content = request.Content,
            IsActive = true
        };

        _repository
            .Setup(r => r.GetByTitleAsync(organizationId, request.Title))
            .ReturnsAsync((Message?)null);

        _repository
            .Setup(r => r.CreateAsync(It.IsAny<Message>()))
            .ReturnsAsync(createdMessage);

        var logic = new MessageLogic(_repository.Object);

        var result = await logic.CreateMessageAsync(organizationId, request);

        result.Should().BeOfType<Created<Message>>();
        var created = (Created<Message>)result;
        created.Value.Id.Should().Be(createdMessage.Id);
        created.Value.OrganizationId.Should().Be(organizationId);
    }

    [Fact]
    public async Task CreateMessageAsync_DuplicateTitle_ReturnsConflict()
    {
        var organizationId = Guid.NewGuid();
        var request = new CreateMessageRequest
        {
            Title = "Duplicate",
            Content = new string('a', 20)
        };

        _repository
            .Setup(r => r.GetByTitleAsync(organizationId, request.Title))
            .ReturnsAsync(new Message { Id = Guid.NewGuid(), OrganizationId = organizationId, Title = request.Title, Content = request.Content });

        var logic = new MessageLogic(_repository.Object);

        var result = await logic.CreateMessageAsync(organizationId, request);

        result.Should().BeOfType<Conflict>();
    }

    [Fact]
    public async Task CreateMessageAsync_InvalidContentLength_ReturnsValidationError()
    {
        var organizationId = Guid.NewGuid();
        var request = new CreateMessageRequest
        {
            Title = "Valid Title",
            Content = "short"
        };

        var logic = new MessageLogic(_repository.Object);

        var result = await logic.CreateMessageAsync(organizationId, request);

        result.Should().BeOfType<ValidationError>();
        var validation = (ValidationError)result;
        validation.Errors.Should().ContainKey("Content");
    }

    [Fact]
    public async Task UpdateMessageAsync_NonExistent_ReturnsNotFound()
    {
        var organizationId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var request = new UpdateMessageRequest
        {
            Title = "Updated",
            Content = new string('a', 20),
            IsActive = true
        };

        _repository
            .Setup(r => r.GetByIdAsync(organizationId, messageId))
            .ReturnsAsync((Message?)null);

        var logic = new MessageLogic(_repository.Object);

        var result = await logic.UpdateMessageAsync(organizationId, messageId, request);

        result.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task UpdateMessageAsync_InactiveMessage_ReturnsValidationError()
    {
        var organizationId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var request = new UpdateMessageRequest
        {
            Title = "Updated",
            Content = new string('a', 20),
            IsActive = true
        };

        _repository
            .Setup(r => r.GetByIdAsync(organizationId, messageId))
            .ReturnsAsync(new Message
            {
                Id = messageId,
                OrganizationId = organizationId,
                Title = "Old",
                Content = new string('a', 20),
                IsActive = false
            });

        var logic = new MessageLogic(_repository.Object);

        var result = await logic.UpdateMessageAsync(organizationId, messageId, request);

        result.Should().BeOfType<ValidationError>();
        var validation = (ValidationError)result;
        validation.Errors.Should().ContainKey("IsActive");
    }

    [Fact]
    public async Task DeleteMessageAsync_NonExistent_ReturnsNotFound()
    {
        var organizationId = Guid.NewGuid();
        var messageId = Guid.NewGuid();

        _repository
            .Setup(r => r.GetByIdAsync(organizationId, messageId))
            .ReturnsAsync((Message?)null);

        var logic = new MessageLogic(_repository.Object);

        var result = await logic.DeleteMessageAsync(organizationId, messageId);

        result.Should().BeOfType<NotFound>();
    }
}
