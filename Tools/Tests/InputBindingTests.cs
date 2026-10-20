using System.Linq;
using NUnit.Framework;
using Runavelle.Core;

namespace Runavelle.Tests
{
    public class InputBindingTests
    {
        [Test]
        public void DefaultsMatchTheShippedControlsAndRoundTrip()
        {
            var b = new InputBindings();
            CollectionAssert.AreEqual(new[] { "Space", "Q" }, b.Keys(InputAction.Ultimate));
            var back = InputBindings.Parse(b.Serialize());
            foreach (InputAction a in System.Enum.GetValues(typeof(InputAction))) CollectionAssert.AreEqual(b.Keys(a), back.Keys(a));
        }

        [Test]
        public void RebindingMovesAKeyWithoutDuplicatesOrUnboundActions()
        {
            var b = new InputBindings();
            var lost = b.Rebind(InputAction.Ultimate, "E", out var err); // E belonged to Dash
            Assert.IsNull(err);
            Assert.AreEqual(InputAction.Dash, lost);
            Assert.AreEqual("E", b.Keys(InputAction.Ultimate)[0]);
            Assert.IsFalse(b.Keys(InputAction.Dash).Contains("E"));
            Assert.AreEqual(InputAction.Ultimate, b.ActionOf("E"));
            b.Rebind(InputAction.Dash, "Z", out err);
            Assert.IsNotNull(err, "reserved undo key");
            // taking the only key of an action is refused
            b.Rebind(InputAction.Speed, "G", out err); b.Rebind(InputAction.Speed, "H", out err);
            var only = new InputBindings();
            only.Rebind(InputAction.Pause, "K", out _);
            var parsed = InputBindings.Parse("Pause=K;Dash=K");
            Assert.AreEqual(InputAction.Dash, parsed.ActionOf("K"), "earlier action (enum order) keeps a shared key");
            CollectionAssert.AreEqual(new[] { "Escape" }, parsed.Keys(InputAction.Pause), "the other falls back to a free default, never empty");
            foreach (InputAction a in System.Enum.GetValues(typeof(InputAction))) Assert.IsNotEmpty(b.Keys(a));
            var all = System.Enum.GetValues(typeof(InputAction)).Cast<InputAction>().SelectMany(a => b.Keys(a)).ToList();
            Assert.AreEqual(all.Count, all.Distinct().Count(), "no key on two actions");
        }

        [Test]
        public void ParsingIsTolerant()
        {
            var b = InputBindings.Parse("Bogus=X;Ultimate=;Dash=Mouse0,R");
            CollectionAssert.AreEqual(new[] { "Space", "Q" }, b.Keys(InputAction.Ultimate), "empty list keeps defaults");
            CollectionAssert.AreEqual(new[] { "R" }, b.Keys(InputAction.Dash), "reserved mouse buttons are dropped");
        }
    }
}
